using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Globalization;

namespace GIS_FIre
{
    class Weather
    {
        /* 虞世宁 6.9 */
        #region 天气
        public class WeatherForecast
        {
            public List<WeatherData> List { get; set; }
        }

        public class WeatherData
        {
            public string dt_txt { get; set; }
            public TemperatureInfo Main { get; set; }
            public List<WeatherInfo> Weather { get; set; }
            public WindInfo Wind { get; set; }
        }

        public class TemperatureInfo
        {
            public double Temp { get; set; }
        }

        public class WeatherInfo
        {
            public string Description { get; set; }
        }

        public class WindInfo
        {
            public double Speed { get; set; }
            public int Deg { get; set; }
        }

        public static async Task<WeatherForecast> GetWeather()
        {
            WeatherForecast forecast = null;
            string apiKey = "3ca7ac536f22a488b84ae14262ed5963";//API Key
            string endpoint = "forecast";
            string city = "Shanghai, CN";
            string units = "metric"; // 使用公制单位

            string url = $"https://api.openweathermap.org/data/2.5/{endpoint}?q={city}&units={units}&appid={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    forecast = JsonConvert.DeserializeObject<WeatherForecast>(responseContent);
                }
            }
            return forecast;
        }
        /*
       /// <summary>
       /// 输出使用实例
       /// </summary>
       /// <returns></returns>
       public static async Task OutputWeaher()
       {
           WeatherForecast forecast= await GetWeather();

           List<WeatherData> weatherData = forecast.List;

           foreach (WeatherData data in weatherData)
           {
               DateTime timestamp = new DateTime(1, 1, 1);
               if (!string.IsNullOrEmpty(data.dt_txt))
               {
                   timestamp = DateTime.ParseExact(data.dt_txt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
               }
               double temperature = data.Main.Temp;
               WeatherInfo weather = data.Weather[0];
               WindInfo wind = data.Wind;
               textBox1.Text += $"{timestamp}: Temperature: {temperature}°C, Weather: {weather.Description}, Wind Speed: {wind.Speed} m/s, Wind Degree: {data.Wind.Deg}°\r\n";
           }
       }
       */

        #endregion
    }
}
