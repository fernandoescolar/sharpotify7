
namespace System.Collections
{
    public class Hashtable : System.Collections.Generic.Dictionary<object, object>
    {
        public Hashtable() : base()
        {

        }
        public Hashtable(int capacity) : base(capacity)
        { 
            
        }
        public new object this[object key]
        {
            get
            {
                if (!ContainsKey(key))
                    return null;
                else return base[key];
            }
            set
            {
                base[key] = value;
            }
        }
    }
}
