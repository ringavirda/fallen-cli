// Vendor namespaces.
using System.Diagnostics;
using FCli.Exceptions;
// FCli namespaces.
using FCli.Models.Types;
using FCli.Services.Abstractions;
using FCli.Services.Tools;
using static FCli.Models.Args;

namespace FCli;

public class PrimesTool : ToolBase
{
    public PrimesTool(
        ICommandLineFormatter formatter,
        IResources resources)
        : base(formatter, resources)
    {
        Description = _resources.GetLocalizedString("Primes_Help");
    }

    // Private data.
    private int _sieveSize;
    private int _sqrtSize;
    private int _parallelization = Environment.ProcessorCount;

    private bool[] _bitArray = null!;

    private bool _time = false;
    private bool _noParallel = false;

    // Overrides.

    public override string Name => "Primes";
    public override string Description { get; }
    public override List<string> Selectors => new()
    {
        "prime", "pr"
    };
    public override ToolType Type => ToolType.Primes;

    protected override void GuardInit()
    {
        // Guard against no arg.
        if (Arg == string.Empty)
        {
            _formatter.DisplayError(Name,
                string.Format(
                    _resources.GetLocalizedString("FCli_ArgMissing"),
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
            _formatter.DisplayError(
                _resources.GetLocalizedString("Primes_NonNumberArg"),
                Name);
            throw new ArgumentException("[Primes] Argument wasn't numeric.");
        }
        // Guard against invalid arg.
        if (_sieveSize < 5)
        {
            _formatter.DisplayError(
                _resources.GetLocalizedString("Primes_InvalidSize"),
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
                    _formatter.DisplayError(Name,
                    _resources.GetLocalizedString("Primes_InvalidParallel"));
                    throw new FlagException("[Primes] Parallel value was invalid.");
                }
                _parallelization = parallelization;
            }
            else
            {
                _formatter.DisplayError(Name,
                    _resources.GetLocalizedString("Primes_NoParallelValue"));
                throw new FlagException("[Primes] Parallel was non numeric.");
            }
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override void Action()
    {
        var cTokenSource = new CancellationTokenSource();
        var cToken = cTokenSource.Token;

        _formatter.DisplayMessage(_resources.GetLocalizedString("FCli_Starting"));
        var draw = _formatter.DrawProgressAsync(cToken);
        draw.Start();
        TimeSpan elapsed = default;

        try
        {
            Task.Run(() =>
            {
                ConsoleKey key = default;
                while (key != ConsoleKey.Q)
                {
                    key = Console.ReadKey(true).Key;
                }
                cTokenSource.Cancel();
                cToken.ThrowIfCancellationRequested();
            }, cToken).ContinueWith((t) =>
            {
                _formatter.DisplayMessage("\r"
                    + _resources.GetLocalizedString("FCli_Cancelled"));
            }, TaskContinuationOptions.OnlyOnCanceled);

            elapsed = Task.Run(() => RunSieve(cToken), cToken).Result;
        }
        // If cancelled, stop execution.
        catch (AggregateException)
        {
            Task.Delay(100).Wait();
            return;
        }
        // Stop operations.
        cTokenSource.Cancel();

        // Count found primes.
        int count = 0;
        for (int i = 0; i < _bitArray.Length; i++)
            if (_bitArray[i]) count++;

        // Display results.
        _formatter.DisplayInfo(Name,
            string.Format(
                _resources.GetLocalizedString("FCli_Results"),
                count,
                _sieveSize));
        if (_time)
            _formatter.DisplayMessage(string.Format(
                _resources.GetLocalizedString("Primes_TimeElapsed"),
                elapsed));
        if (_noParallel)
            _formatter.DisplayMessage($"No-Parallel: {_noParallel}");
    }

    private TimeSpan RunSieve(CancellationToken cancellationToken)
    {
        var watch = new Stopwatch();
        // If measuring time start clock.
        if (_time) watch.Start();
        // We start from number 3, since 0 an 1 are primes and we omit all even
        // numbers from the sieve.
        int factor = 3;

        // Cross primes up to sqrt of sieve size, since there aren't any primes
        // after that point.
        while (factor < _sqrtSize)
        {
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
                var segmentSize = ((_sieveSize - factorSqr) / _parallelization) + 1;
                // Cross all factors of found number. This part can be parallel.
                var options = new ParallelOptions()
                {
                    CancellationToken = cancellationToken
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
        // Return TimeSpan regardless.
        return watch.Elapsed;
    }
}
