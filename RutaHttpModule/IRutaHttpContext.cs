using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutaHttpModule
{
    internal interface IRutaHttpContext
    {
        bool IsAuthenticated { get; }
        string DomainUserName { get; }
        void RemoveRequestHeader(string header);
        void AddRequestHeader(string header, string value);
    }
}
