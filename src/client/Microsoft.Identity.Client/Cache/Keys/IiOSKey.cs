namespace Microsoft.Identity.Client.Cache.Keys
{
    internal interface IiOSKey
    {
        string iOSAccount { get; }

        string iOSGeneric { get; }

        string iOSService { get; }

        int iOSType { get; }
    }
}
