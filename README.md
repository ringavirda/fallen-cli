# Summary
## Basic
### Tools:
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

### Shell types:
Bash, Powershell, Cmd.

### Supported command types:
Executable, Website, Script.

## Extended
### Tools:
config - manipulates dynamic configuration.
- locale - overrides system local.
- formatter - 
    - inline - formats messages as is. 
    - pretty - prettifies messages with colors and additional info. 
- purge - deletes persisted user config (requires confirmation).

group - manipulates command groups (requires name).
- name - specify group name.
- remove - removes group (confirmation).
- override - overrides existing command (confirmation).
- yes - skips confirmation.

change - manipulates a known command (displays command info if no flag is given).
- name - changes name to given.
- path - changes path to given.
- options - changes options to given.
- type - changes type (confirmation).

### Shell types:
Fish.

### Supported command types:
Dir, Shell.

# Supported command types:
- exe - Windows executable (Linux treats as script).
- url - opens link in the default browser.
- script - executes as a specified shell script (requires supported shell type as value).
- shell - considered a shell command (one line, requires supported shell).
- dir - file system folder (assumes explorer if nothing is specified).
    - explorer - opens in default file explorer.
    - (supported shell name) - opens in specified shell.

# Supported shell types
- bash - standard Linux shell (Windows via WSL).
- powershell - new Windows shell (Optionally available on Linux).
- cmd - old Windows shell (not on Linux).
- fish - optional Linux shell (not on Windows).

# Examples

fcli add C:\awesome --exe \
fcli add C:\directory \
fcli add C:\directory --dir bash \
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

fcli config --locale ua --formatter pretty \
fcli config --purge --yes

fcli group "awesome google" --name some-name --override \
fcli group some-name --remove --yes \
fcli group --name some-group --remove

fcli change awesome --name new-name \
fcli ch google

# Defined objects

Flag - pair of key and value (flag param). \
ParsedArgs - contains possible selector, possible arg, and Flags. \

Tool - represents action that manipulates known commands. Requires arg and Flags.
Command - contains a user-defined action. Requires arg.

Group - a collection of commands that need to be executed sequentially.

# Extended features
Logging, localization, testability.
