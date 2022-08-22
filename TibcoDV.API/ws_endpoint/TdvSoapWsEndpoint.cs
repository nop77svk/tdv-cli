namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NoP77svk.Web.WS;

    public class TdvSoapWsEndpoint<TContentType> : SoapWsEndpoint<TContentType>
    {
        private bool isXmlNamespaceUriUsed = false;

        public TdvSoapWsEndpoint(string soapAction, TContentType content)
            : base(soapAction, content)
        {
        }

        public override string UriResource
        {
            get
            {
                OverrideResourcePathFromXmlNamespace();

                if (Resource is null || !Resource.Any())
                {
                    return string.Empty;
                }
                else
                {
                    return IWebServiceEndpoint.ResourceToString(Resource
                        .Prepend("services")
                        .Append(Resource.Last() + "Port.ws")
                    );
                }
            }
        }

        private void OverrideResourcePathFromXmlNamespace()
        {
            if (isXmlNamespaceUriUsed)
                return;

            string? reflectedNamespace = ReflectContentTypeForXmlSerializerNamespace();
            if (!string.IsNullOrWhiteSpace(reflectedNamespace))
            {
                const string XmlNamespaceUriPrefix = "http://www.compositesw.com/services/";

                string namespaceWithoutDomain = reflectedNamespace;
                if (reflectedNamespace.StartsWith(XmlNamespaceUriPrefix, StringComparison.OrdinalIgnoreCase))
                    namespaceWithoutDomain = reflectedNamespace[XmlNamespaceUriPrefix.Length..];

                if (namespaceWithoutDomain != reflectedNamespace)
                {
                    Resource?.Clear();
                    IEnumerable<string> resourceFolders = namespaceWithoutDomain.Split('/');
                    foreach (string resourceFolder in resourceFolders)
                        AddResourceFolder(resourceFolder);
                }
            }

            isXmlNamespaceUriUsed = true;
        }
    }
}
