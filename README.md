# Delta Hedging - Simplify your Option Pricing


## Table of Content

* [Overview](#overview)

* [Disclaimer](#disclaimer)

* [Prerequisites](#prerequisites)

* [Setup](#setup)


## <a id="overview"></a>Overview
In this example, I will present different workflows used to manage risk, specifically focusing on the most widely used Greek (Delta), as a tool to hedge common strategies.

Details and concepts are further explained in the [Delta Hedging - Simplify your Option Pricing]() article published on the [Refinitiv Developer Community portal](https://developers.refinitiv.com).

## <a id="disclaimer"></a>Disclaimer
The source code presented in this project has been written by Refinitiv only for the purpose of illustrating the concepts of creating the "what-if" scenarios using the Refinitiv Data Platform Library for .NET.  It has not been tested for usage in production environments. 

***Note 1:** The Refinitiv Data Platform Library for .NET is a community-based API and managed as an open source project.*

***Note 2:** To be able to [ask questions](https://community.developers.refinitiv.com/index.html) and to benefit from the full content available, I recommend you to register on the [Refinitiv Developer Community](https://developers.refinitiv.com)*

## <a name="prerequisites"></a>Prerequisites

Software components used:

- [Refinitiv Data Platform](https://developers.refinitiv.com/refinitiv-data-platform/refinitiv-data-platform-apis): Access to the [pricing endpoint](https://api.refinitiv.com/) data services
- .NET Environment: 
  - Tested with .Net Core 3 (Visual Studio 2019)
  - NuGet: [Refinitiv DataPlatform.Content](https://www.nuget.org/packages/Refinitiv.DataPlatform.Content/1.0.0-alpha)
  - RDP for .NET installation: Installed as part of the example package within Visual Studio

## <a name="setup"></a>Setup

The application package includes the following:
* **Delta Hedging.sln**
  
  The Visual Studio Project file containing all source code and dependencies to execute the application.

  * **Platform Access**
    
    Within the project, the Configuration project defines the Credentials.cs file allow the specification of your platform session credentials to access the content.  
    
    **Note**: Access credentials to Pricing and Analytics data services are required.
    
    ```c#
    public static class Credentials
    {
        // ********************************************************************
        // RDP/ERT in Cloud Global Authentication parameters
        //
        // Note: Parameters in this section are only applicable if you were
        //       provided RDP or ERT in Cloud credentials.
        // ********************************************************************
        public static string RDPUser { get; } = "<YOUR MACHINE ID>";
        public static string RDPPassword { get; } = "<PASSWORD>";
    
        // AppKey used for both Desktop or Platform session types.
        public static string AppKey { get; } = "<YOUR APP KEY>";
    }
    ```

## Running the application

The application has been tested and executed within the `Visual Studio 2019` IDE.  Prior to running the application, ensure you have supplied your access credentials - refer to the Setup section above.

### <a id="contributing"></a>Contributing

Please read [CONTRIBUTING.md](https://gist.github.com/PurpleBooth/b24679402957c63ec426) for details on our code of conduct, and the process for submitting pull requests to us.

### <a id="authors"></a>Authors

* **Nick Zincone** - Release 1.0.  *Initial version*

### <a id="license"></a>License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

