# Dynamic OData Query Library

## Overview
Dynamic OData is a query library built upon [OData Web API](https://github.com/OData/WebApi) that allows you to query dynamically created
Entity Data Models.

OData expects the return schema to be static at compile time, there are scenarios where applications would want to construct the return response on the go.
This library helps to achieve that with configurable model and providing metadata which are used at runtime to create dynamic response schema.

This provides flexibity to have a dynamic schema and still enable the OData magic to work. The library enables you to construct a Controller method of IEnumerable < IEdmEntityObject > return type and then construct this Object using a mapped Dictionary.


## Installation
To install this library, please download the latest version of  [NuGet Package](https://www.nuget.org/packages/dynamic-odata) from [nuget.org](https://www.nuget.org/) and refer it into your project.  

## How to use 

Refer to the samples at https://github.com/microsoft/dynamic-odata/blob/main/samples


## Testing

## Benchmark

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
