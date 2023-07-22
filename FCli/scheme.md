# Tools
Tools are there to manipulate commands.

add - appends new command to the known commands list and persists it. \
remove - deletes command from persistanse. \
list - displays all known commands. \
run - execute specified command or path. Does not persist the action.

# Commands

Defined by user and persisted in a json format. Can be invoked by calling fcli and specifying the name of the command.

Examples:

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
