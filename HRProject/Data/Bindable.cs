using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EazyCash.Data
{
    public class Bindable : INotifyPropertyChanged
    {
        private Dictionary<string, object> _properties = new Dictionary<string, object>();
      
        public bool IsDirty;
        /// <summary>
        /// Gets the value of a property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        protected T Get<T>([CallerMemberName] string name = null , T mvalue= default(T))
        {
           // Debug.Assert(name != null, "name != null");
            object value = null;
           // if (_properties.TryGetValue(name, out value))
                if (_properties.Keys.Contains(name))
                {
                //if (_properties.TryGetValue(name, out value)) ;
                //return value == null ? default(T) : (T)value;
                return (T)_properties[name];

            }
                else
                {
                    _properties.Add(name, mvalue);
                return mvalue;
                }

                
           // return default(T);
        }

        /// <summary>
        /// Sets the value of a property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <remarks>Use this overload when implicitly naming the property</remarks>
        protected void Set<T>(T value, [CallerMemberName] string name = null)
        {
           // Debug.Assert(name != null, "name != null");
            if (Equals(value, Get<T>(name, value)))
                return;
            _properties[name] = value;
            OnPropertyChanged(name);
            IsDirty = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler? handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class test : Bindable
    {
        public string FirstName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
    }

}
