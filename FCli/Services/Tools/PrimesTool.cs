using System.Diagnostics;
using System.Globalization;

using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Types;
using FCli.Services.Abstractions;

namespace FCli.Services.Tools;

public class PrimesTool : ToolBase
{
    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public PrimesTool() : base()
    {
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>   
    public PrimesTool(
        ICommandLineFormatter formatter,
        IResources resources)
        : base(formatter, resources)
    {
        Description = Resources.GetLocalizedString("Primes_Help");
    }

    // Private data.
    private int _sieveSize;
    private int _sqrtSize;
    private int _parallelization = Environment.ProcessorCount;

    private volatile bool[] _bitArray = null!;

    private bool _time;
    private bool _noParallel;

    // Overrides.

    public override string Name => "Primes";
    public override string Description { get; }
    public override List<string> Selectors => new()
    {
        "primes", "pr"
    };
    public override ToolType Type => ToolType.Primes;

    protected override void GuardInit()
    {
        // Guard against no arg.
        if (Arg == string.Empty)
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_ArgMissing"),
                    Name));
            throw new ArgumentException("[Primes] No argument was given.");
        }
        // Guard against non number arg.
        if (int.TryParse(Arg, out var size))
        {
            _sieveSize = size;
            //
            _sqrtSize = (int)Math.Sqrt(size);
            _bitArray = new bool[(_sieveSize + 1) / 2];
            Array.Fill(_bitArray, true);
        }
        else
        {
            Formatter.DisplayError(
                Resources.GetLocalizedString("Primes_NonNumberArg"),
                Name);
            throw new ArgumentException("[Primes] Argument wasn't numeric.");
        }
        // Guard against invalid arg.
        if (_sieveSize < 5)
        {
            Formatter.DisplayError(
                Resources.GetLocalizedString("Primes_InvalidSize"),
                Name);
            throw new ArgumentException("[Primes] Sieve size was invalid.");
        }
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // Display execution time as well.
        if (flag.Key == "time")
        {
            FlagHasNoValue(flag, Name);
            _time = true;
        }
        // Use single thread.
        else if (flag.Key == "no-parallel")
        {
            FlagHasNoValue(flag, Name);
            _noParallel = true;
        }
        else if (flag.Key == "parallel")
        {
            FlagHasValue(flag, Name);
            // Guard against invalid parallel value.
            if (int.TryParse(flag.Value, out var parallelization))
            {
                if (parallelization < 1)
                {
                    Formatter.DisplayError(
                        Name,
                        Resources.GetLocalizedString("Primes_InvalidParallel"));
                    throw new FlagException(
                        "[Primes] Parallel value was invalid.");
                }
                _parallelization = parallelization;
            }
            else
            {
                Formatter.DisplayError(
                    Name,
                    Resources.GetLocalizedString("Primes_NoParallelValue"));
                throw new FlagException("[Primes] Parallel was non numeric.");
            }
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override async Task ActionAsync()
    {
        // Cancel setup.
        var cTokenSource = new CancellationTokenSource();
        var cToken = cTokenSource.Token;

        // Start up.
        Formatter.DisplayInfo(
            Name,
            Resources.GetLocalizedString("Primes_Starting"));
        TimeSpan elapsed = default;
        Formatter.DrawProgressAsync(cToken).Start();

        try
        {
            _ = Task.Run(() =>
            {
                // Watch for keys.
                ConsoleKey key = default;
                while (key != ConsoleKey.Q)
                {
                    key = Console.ReadKey(true).Key;
                }
                // Cancel if q was pressed.
                Formatter.DisplayProgressMessage(
                    Resources.GetLocalizedString("FCli_Cancelled"));
                cTokenSource.Cancel();
            }, cToken);

            elapsed = await RunSieveAsync(cToken);
        }
        // If cancelled, stop execution.
        catch (AggregateException)
        {
            return;
        }
        // Stop operations.
        cTokenSource.Cancel();

        // Count found primes.
        int count = _bitArray.AsParallel().Count(b => b);

        // Display results.
        Formatter.DisplayInfo(
            Name,
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.GetLocalizedString("Primes_Results"),
                count,
                _sieveSize));
        if (_time)
            Formatter.DisplayMessage(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Primes_TimeElapsed"),
                    elapsed));
        if (_noParallel)
            Formatter.DisplayMessage($"No-Parallel: {_noParallel}");
    }

    /// <summary>
    /// Runs sieve in a separate task.
    /// </summary>
    /// <param name="cToken">Used to cancel operation.</param>
    /// <returns>Elapsed time.</returns>
    private Task<TimeSpan> RunSieveAsync(CancellationToken cToken)
        => Task.Run(() =>
        {
            var watch = new Stopwatch();
            // If measuring time start clock.
            if (_time) watch.Start();
            // We start from number 3, since 0 an 1 are primes and we omit all 
            // even numbers from the sieve.
            int factor = 3;

            // Cross primes up to sqrt of sieve size, since there aren't any 
            // primes after that point.
            while (factor < _sqrtSize)
            {
                // Enable cancellation.
                cToken.ThrowIfCancellationRequested();
                // Search for the next prime.
                // Increment by 2 to skip all even numbers.
                for (int next = factor; next <= _sqrtSize; next += 2)
                {
                    // If prime, break.
                    if (_bitArray[next >> 1])
                    {
                        factor = next;
                        break;
                    }
                }
                // Factor squared is a starting point for crossing off factors.
                var factorSqr = factor * factor;
                // Use single thread if no parallel.
                if (_noParallel)
                {
                    for (
                        int next = factorSqr;
                        next <= _sieveSize;
                        next += factor * 2)
                    {
                        _bitArray[next >> 1] = false;
                    }
                }
                // Utilize parallel.
                else
                {
                    // Calculate parallel segments.
                    var segmentSize =
                        ((_sieveSize - factorSqr) / _parallelization) + 1;
                    // Cross all factors of found number. This part can be parallel.
                    var options = new ParallelOptions()
                    {
                        CancellationToken = cToken
                    };
                    Parallel.For(0, _parallelization, options, (segment) =>
                    {
                        // In tread image of bit array.
                        var inner = _bitArray.AsSpan();
                        // Calculate starting location.
                        var start = factorSqr + segment * segmentSize;
                        // Guard against last segment.
                        var end = Math.Min(start + segmentSize, _sieveSize);
                        // Find out actual starting point.
                        int startPos = Math.Max((start / factor) | 1, factor);
                        // Cross off the list all multiples ignoring even ones.
                        for (
                            int next = startPos * factor;
                            next < end;
                            next += factor * 2)
                        {
                            inner[next >> 1] &= false;
                        }
                    });
                }
                // Propagate to the next potential prime. 
                factor += 2;
            }
            // If time stop watch.
            if (_time) watch.Stop();
            // Report.
            Formatter.DisplayProgressMessage(
                Resources.GetLocalizedString("Primes_Completed"));
            // Return TimeSpan regardless.
            return watch.Elapsed;
        }, cToken);
}