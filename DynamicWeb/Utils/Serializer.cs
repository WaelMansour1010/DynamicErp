using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MyERP
{
    public static class Serializer<T>
    {
        #region Private Fields

        #endregion

        #region Const.

        static Serializer()
        {
        }

        #endregion

        #region Public Methods

        public static string Serialize(T obj)
        {
            if (obj == null)
            {
                return null;
            }

            return JsonConvert.SerializeObject(obj);
        }
        public static string Serialize(T obj, bool camelCase)
        {
            if (obj == null)
            {
                return null;
            }
            if (camelCase == true)
            {
                var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
            }
            else
            {
                return JsonConvert.SerializeObject(obj);
            }
        }
        public static T Desrialize(string json)
        {
            if (json == null)
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        #endregion
    }
}