using System.Collections.Generic;

namespace PowerupsLite.When
{
    public class Array : Dictionary<object, object> {

        public static Dictionary<string, Array> Arrays { get; set; } = new Dictionary<string, Array>();
    }

}
