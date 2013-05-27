using System.Configuration;
using System.Data.Common;

namespace CTX.WinscpExtensions
{
    public class ConnStringInfo
    {
        private readonly DbConnectionStringBuilder _connString;

        public ConnStringInfo(string connStringOrName)
        {
            var b = new DbConnectionStringBuilder();

            try
            {
                b.ConnectionString = ConfigurationManager.ConnectionStrings[connStringOrName].ConnectionString;
            }
            catch
            {
                b.ConnectionString = connStringOrName;
            }

            _connString = b;
        }

        public string this[string key]
        {
            get { return _connString.ContainsKey(key) ? (string)_connString[key] : null; }
        }
    }
}