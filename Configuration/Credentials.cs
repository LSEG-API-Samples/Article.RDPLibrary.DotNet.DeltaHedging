namespace Configuration
{
    // GlobalSettings
    // The following class is a convenient interface used strictly for the Examples within this solution.  Most of the examples,
    // will refer to these global settings allowing the developer to easily test across all provided example projects.
    //
    // Depending on the credentials provided to you, modify the specific section outlined below.
    //
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
}

