# FCli API Documentation  
This dotnet tool uses standard service-based architecture.

## Namespaces
- FCli - root namespace.
- FCli.Exceptions - holds templates for the common errors in the fcli.
- FCli.Models - has most of the data classes that represent main business objects.
- FCli.Models.Dto - contains translation objects that are used during operations such as creation or alter.
- FCli.Models.Identity - provides classes for user and contact management.
- FCli.Models.Types - represents known enumerations.
- FCli.Services - contains top-level implementations of the abstracts.
- FCli.Services.Abstractions - holds interfaces that define the api of the services.
- FCli.Services.Config - classes for the user configuration.
- FCli.Services.Data - object persistance subsystem.
- FCli.Services.Data.Identity - user management addition for the persistance.
- FCli.Services.Encryption - has known encrypters for the sensitive user data.
- FCli.Services.Format - provides services for command line messages formatting.
- FCli.Services.Tools - main namespace for the fcli tools.