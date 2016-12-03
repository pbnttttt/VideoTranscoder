using HandBrake.Interop;
using HandBrake.Interop.Model;
using HandBrake.Interop.Model.Encoding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HandBrake
{
    class Program
    {
        private static HandBrakeInstance instance;
        static string srcPath = @"D:\Work\ULT126.ISO";
        static string destDir = @"D:\Work\Output";
        static int[] bitrates = { 400, 800, 1600, 2400 };

        static void Main(string[] args)
        {
            instance = new HandBrakeInstance();

            instance.Initialize(verbosity: 1);
            instance.ScanCompleted += Instance_ScanCompleted;
            instance.StartScan(srcPath, previewCount: 10);

            Console.ReadLine();
        }

        private static void Instance_ScanCompleted(object sender, EventArgs e)
        {
            var source = sender as HandBrakeInstance;
            if (source.Titles.Count == 0)
            {
                Console.WriteLine("找無標題，ISO 無效");
                return;
            }

            foreach (var bitrate in bitrates)
            {
                var src = new FileInfo(srcPath);
                var destfileName = $"{Path.GetFileNameWithoutExtension(srcPath)}_{bitrate}.mp4";
                var destPath = Path.Combine(destDir, destfileName);

                EncodingProfile profile = GetProfile(bitrate);
                var job = new EncodeJob
                {
                    SourcePath = srcPath,
                    OutputPath = destPath,
                    EncodingProfile = profile,
                    Title = instance.FeatureTitle,
                    SecondsStart = TimeSpan.FromMinutes(10).TotalSeconds,
                    SecondsEnd = TimeSpan.FromMinutes(11).TotalSeconds,
                    RangeType = VideoRangeType.Seconds,
                    //RangeType = VideoRangeType.All,
                    ChosenAudioTracks = new List<int> { 1 },
                    Subtitles = new Subtitles
                    {
                        SourceSubtitles = new List<SourceSubtitle>(),
                        SrtSubtitles = new List<SrtSubtitle>()
                    },
                };

                instance.EncodeProgress += (o, args) =>
                {
                    Console.WriteLine($"{destPath}\t{args.FractionComplete}");
                };
                instance.EncodeCompleted += (o, args) =>
                {
                    Console.WriteLine("Encode completed.");
                };
                instance.StartEncode(job);
            }

            Console.ReadLine();
            Console.WriteLine("Done");
        }

        private static EncodingProfile GetProfile(int bitrate)
        {
            EncodingProfile profile;
            ASP asp = GetASP(bitrate);

            var serializer = new XmlSerializer(typeof(EncodingProfile));
            using (var stream = new FileStream("Normal.xml", FileMode.Open, FileAccess.Read))
            {
                profile = serializer.Deserialize(stream) as EncodingProfile;
            }

            profile.VideoEncodeRateType = VideoEncodeRateType.AverageBitrate;
            profile.Anamorphic = Anamorphic.None;
            profile.KeepDisplayAspect = true;
            profile.VideoBitrate = bitrate;
            profile.Width = asp.Width;
            profile.Height = asp.Height;

            return profile;
        }

        private static ASP GetASP(int bitrate)
        {
            ASP result = new ASP();
            switch (bitrate)
            {
                case 400:
                    result.Width = 320;
                    result.Height = 180;
                    break;
                case 800:
                    result.Width = 416;
                    result.Height = 238;
                    break;
                case 1600:
                    result.Width = 712;
                    result.Height = 400;
                    break;
                case 2400:
                    result.Width = 1280;
                    result.Height = 720;
                    break;
                default:
                    throw new ArgumentException(nameof(bitrate));
            }
            return result;
        }
    }

    public class ASP
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
