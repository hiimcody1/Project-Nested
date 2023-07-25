using Project_Nested.Injection;
using System;
using System.IO;
using System.Reflection;

namespace Project_Nested {
    internal class CLI {
        private static String filename;
        private static Boolean debug;
        private static Injector injector;
        public static void Main(string[] args) {
            Console.WriteLine("Nested");
            HandleArgs(args);
            if (LoadNesFile(filename)) {
                var lines = File.ReadAllText("z2r.txt");
                
                injector.ResetSettings();
                injector.SetAllSettings(lines);
                injector.SetAllSettings(File.ReadAllLines("GlobalSettings.config"));

                LoadSRMs();
                SaveSnesSync();
                Console.WriteLine("Successfully Created ROM!");
            } else {
                Console.Error.WriteLine("Couldn't load ROM!");
            }
        }

        private static void HandleArgs(String[] args) {
            for (int i = 0; i < args.Length; i++) {
                switch (args[i]) {
                    case "--help":
                        Console.WriteLine(Assembly.GetExecutingAssembly().GetName() + " --rom <path to Z2R rom>");
                        break;
                    case "--rom":
                        if (args[i + 1].StartsWith("--"))
                            throw new Exception("No Rom Specified!");
                        filename = args[i + 1];
                        break;
                    case "--debug":
                        debug = true;
                        break;
                    default:
                        //Likely an option value
                        break;
                }
            }
            if (filename == null) {
                Console.Error.WriteLine("Must supply --rom");
                throw new Exception("Wrong Args");
            }
        }

        private static Boolean LoadNesFile(string filename) {
            injector = new Injector(File.ReadAllBytes(filename));
            return injector.IsLoaded(false);
        }

        private static void LoadSRMs() {
            foreach (var file in Directory.GetFiles("./srm/")) {
                // Read file and find header
                byte[] data = File.ReadAllBytes(file);
                SrmFeedbackReader reader = new SrmFeedbackReader(injector, data);

                // Is this a valid save file?
                if (reader.IsValid) {

                    // Read feedback
                    var calls = reader.GetFunctionEntryPoints();
                    var callsString = injector.ConvertCallsToString(calls);

                    // Apply feedback
                    injector.SetSetting(callsString);
                }
            }
        }
        private static string SaveSnesSync() {
            if (injector == null)
                return null;

            if (!injector.mapperSupported) {
                Console.WriteLine($"Mapper {injector.ReadMapper()} isn't supported.");
                return null;
            }

            //try
            {
                var fullFileName = filename + ".smc";

                var data = injector.FinalChanges(null, null);
                File.WriteAllBytes(fullFileName, data);

                Console.WriteLine("Saving done!");

                return fullFileName;
            }
            /*catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "Error!");
            }*/
            return null;
        }
    }
}
