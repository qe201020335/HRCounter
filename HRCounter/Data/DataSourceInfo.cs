using System;
using System.Threading.Tasks;


namespace HRCounter.Data
{
    public readonly struct DataSourceInfo
    {
        public string Key { get; }
        public Type DataSourceType { get; }
        
        private readonly Func<Task<string>> _sourceLinkTextAction;
        
        private readonly Func<bool> _precondition;

        internal DataSourceInfo(string key, Type dataSourceType, Func<Task<string>> sourceLinkTextCallback,
            Func<bool> precondition)
        {
            Key = key;
            DataSourceType = dataSourceType;
            _sourceLinkTextAction = sourceLinkTextCallback;
            _precondition = precondition;
        }


        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is DataSourceInfo other))
            {
                return false;
            }
            
            // compare elements here
            return other.Key == Key && other.DataSourceType == DataSourceType;
        }
        
        public override int GetHashCode()
        {
            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
            unchecked // Overflow is fine, just wrap
            {
                var hash = 17;
                hash = hash * 23 + Key.GetHashCode();
                hash = hash * 23 + DataSourceType.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Key} ({DataSourceType.Name})";
        }

        internal async Task<string> GetSourceLinkText()
        {
            return await _sourceLinkTextAction();
        }

        internal bool PreconditionSatisfied()
        {
            return _precondition();
        }
    }
}