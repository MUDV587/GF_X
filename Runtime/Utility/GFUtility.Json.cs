public static partial class GFUtility
{
    public class Json
    {
        public static string ToJson(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public static T ToObject<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
        public static object ToObject(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        }
    }
}