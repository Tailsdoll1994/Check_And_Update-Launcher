using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CheckAndUpdate
{
    class Program
    {
        static void Read()
        {
            try
            {
                using (FileStream fstream = File.OpenRead("version.txt"))
                {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string textFromFile = Encoding.Default.GetString(array);
                    Console.WriteLine("Проверка версии {0}", textFromFile);
                }
            }
            catch
            {
                Console.WriteLine("Не найден путь к файлу");
            }
            Update();
        }
        static async void Update()
        {
            try
            {
                string post = "{\"project\":\"forge\"}";
                Console.WriteLine(post);
                using (var Http = new HttpClient())
                {
                    var respon = await Http.PostAsync("https://api.cakeproject.ru/game/get_version",
                        new StringContent(post, Encoding.UTF8, "application/json"));

                    // если respon успешно получает IsSuccessStatusCode (http-ответ) 
                    if (respon.IsSuccessStatusCode)
                    {
                        //1. объявление переменой
                        //2. Content возвращает, задает или указывает содержимое http-сообщения
                        var ResponContent = respon.Content;

                        //1. объявление переменой
                        //2. сериализация содержимиого http-сообщения в строку 
                        //3. Result, получение итого значения http-сообщения
                        string responseString = ResponContent.ReadAsStringAsync().Result;
                        SortedDictionary<string, string> list = JsonConvert.DeserializeObject<SortedDictionary<string, string>>(responseString);
                        string textFromFile;
                        using (FileStream fstreamread = File.OpenRead("version.txt"))
                        {
                            byte[] array = new byte[fstreamread.Length];
                            fstreamread.Read(array, 0, array.Length);
                            textFromFile = Encoding.Default.GetString(array);

                            if (list["error"] != "")
                            {
                                Console.WriteLine(list["error"]);
                                return;
                            }
                        }
                        if (textFromFile != list["version"])
                        {
                            Console.WriteLine("Ответ от сервера: {0}", responseString);
                            using (FileStream fstreamsave = File.OpenWrite("version.txt"))
                            {
                                byte[] json_byte = Encoding.Default.GetBytes(list["version"].Replace("version:", ""));
                                fstreamsave.Write(json_byte, 0, json_byte.Length);
                            }
                            DownloadFile(list["url"]);
                        }
                        else
                        {
                            Console.WriteLine("У вас установлена последняя версия");
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Не возможно получить ответ с указаного сайта");
            }
        }
        public static async void DownloadFile(string url)
        {
            Console.WriteLine("Производим загрузку обновления, пожалуйста подождите");
            byte[] data;
            using (var client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                data = await content.ReadAsByteArrayAsync();
                using (FileStream file = File.Create("update.zip"))
                    file.Write(data, 0, data.Length);
            }
            Console.WriteLine("Загрузка завершена");
            UnzipFile();

        }
        static void UnzipFile()
        {
            if (File.Exists("Game"))
            {
                File.Delete("Game");
            }
            if (!File.Exists("update.zip"))
            {
                Console.WriteLine("Указанный файл не найден.");
                return;
            }
            Console.WriteLine("Начинаем распаковку...");

            using (ZipFile zipFile = new ZipFile("update.zip"))
            {
                zipFile.ExtractAll("Game");
                ICollection<ZipEntry> files = zipFile.Entries;
                foreach (ZipEntry entry in files)
                {
                    Console.WriteLine(entry.FileName);
                    if (entry.FileName.Substring(entry.FileName.Length - 4) == ".exe")
                    {
                        Process.Start(Environment.CurrentDirectory + "\\Game\\" + entry.FileName);
                        break;
                    }
                }
                Console.WriteLine("Готово!");
            }
            File.Delete("update.zip");
            Environment.Exit(0);
        }
        static void Main(string[] args)
        {
            if (!File.Exists("version.txt"))
            {
                using (FileStream create = File.Create("version.txt"))
                {
                    byte[] version = Encoding.Default.GetBytes("forge");
                    create.Write(version, 0, version.Length);
                    Console.WriteLine("Пожалуйста перезапустите программу");
                }
            }
            else
            {
                Read();
            }
            Console.ReadKey();
        }
    }
}
