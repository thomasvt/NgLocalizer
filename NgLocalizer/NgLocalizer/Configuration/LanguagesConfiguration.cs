using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace NgLocalizer.Configuration
{
    public class LanguagesConfiguration
    {
        private List<string> _languages = [];

        public void Load()
        {
            _languages = ConfigurationManager.AppSettings["languages"].ToLower().Split(',').ToList();
        }

        public IEnumerable<string> AllLanguages => _languages;
    }
}
