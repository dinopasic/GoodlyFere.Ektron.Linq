# GoodlyFere.Ektron.Linq Documentation

## Summary 

Linq to Ektron AdvancedSearchCriteria search provider.

## Basic Usage
   
    var widgets = EktronQueryFactory.Queryable<Widget>(new AppSettingsIdProvider());

    var query = from w in widgets
                where w.Id > 10
                select w;

    Widget[] itemWidgets = query.ToArray();
    foreach (var widget in itemWidgets)
    {
        Console.WriteLine("Widget found: {0}", widget.Id);
    }

    Console.ReadKey();

## Widget Class
    public class Widget
    {
        #region Public Properties

        [EktronProperty(EkConstants.SearchProperties.ContentId,
            EktronExpressionType = typeof(IntegerPropertyExpression))]
        public long Id { get; set; }

        public string Name { get; set; }
            
        #endregion
    }

## AppSettingsIdProvider Class

    public class AppSettingsIdProvider : IEktronIdProvider
    {
        #region Public Methods

        public long GetContentTypeId(string name)
        {
            EnsureValidName(name);
            string key = String.Concat(name, "ContentType");
            return GetId(key);
        }

        public long GetSmartFormId(string name)
        {
            EnsureValidName(name);
            string key = String.Concat(name, "SmartForm");
            return GetId(key);
        }

        #endregion

        #region Methods

        private static long GetId(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (String.IsNullOrEmpty(value))
            {
                throw new ArgumentOutOfRangeException(
                    "key", String.Format("Could not find {0} in application settings.", key));
            }

            return Int64.Parse(value);
        }

        private void EnsureValidName(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
        }

        #endregion
    }