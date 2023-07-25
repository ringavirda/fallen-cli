# Summary
## Tools:
add - appends new command to the known commands list and persists it.
- name - explicitly specifies command name.
- options - adds command line options (ignored for urls).
- (supported command type) - explicitly specifies command type.

remove - deletes command from persistance.
- all - removes all known commands (confirmation mandatory).
- yes - skip confirmation for one single command delete.

list - displays all known commands (uses arg as filter and shows all commands if no flag given).
- script - displays all scripts.
- url - displays all websites.
- exe - displays all executables.

run - execute specified command or path, but does not persist the action (requires command type explicitly specified).
- exe - run as executable.
- url - run as website.
- script - run as a shell script (requires supported shell type as value).

## Shell types:
Bash, Powershell, Cmd.

## Supported command types:
Executable, Website, Script.


# Supported command types:
- exe - Windows executable (Linux treats as script).
- url - opens link in the default browser.
- script - executes as a specified shell script (requires supported shell type as value).

# Supported shell types
- bash - standard Linux shell (Windows via WSL).
- powershell - new Windows shell (Optionally available on Linux).
- cmd - old Windows shell (not on Linux).

# Examples

fcli add C:\awesome --exe \
fcli add C:\awesome.exe \
fcli add C:/awesome --script bash \
fcli add c:/awesome.ps1 --script \
fcli add "http://awesome" --url \
fcli add https://awesome.com

fcli list \
fcli list awesome \
fcli list --scripts

fcli remove awesome \
fcli remove awesome --yes

fcli run "http://awesome" --url \
fcli run C:/awesome --script bash

# Defined objects

Flag - pair of key and value (flag param). \
ParsedArgs - contains possible selector, possible arg, and Flags. \

Tool - represents action that manipulates known commands. Requires arg and Flags.
Command - contains a user-defined action. Requires arg.
