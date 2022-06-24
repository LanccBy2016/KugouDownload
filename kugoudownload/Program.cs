using Furion;
using Furion.ClayObject;
using Furion.DataEncryption;
using Furion.JsonSerialization;
using Furion.RemoteRequest.Extensions;
using Furion.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Profiling.Data;
using System.Security.Cryptography.Xml;
using System.Text;


var services = Inject.Create();
// 注册服务
services.AddRemoteRequest();
services.Build();

var searchKeyWord = string.Empty;
int writeIndex = 0;


var versionInfo = TP.Wrapper("酷狗音乐下载器",
    "[作者] cc",
    "[时间] 2022-06-24"
    );
Console.WriteLine(versionInfo);



Console.WriteLine("输入关键词:");
searchKeyWord = Console.ReadLine();
string t = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds.ToString();
var headers = new Dictionary<string, object> {
        { "User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36" }
    };

string[] sign_params = {"NVPh5oo715z5DIWAeQlhMDsWXXQV4hwt", "bitrate=0", "callback=callback123",
                       "clienttime=" + t, "clientver=2000", "dfid=-", "inputtype=0", "iscorrection=1",
                       "isfuzzy=0",
                       "keyword=" + searchKeyWord, "mid=" + t, "page=" + 1, "pagesize=30",
                       "platform=WebFilter", "privilege_filter=0", "srcappid=2919", "token=", "userid=0",
                       "uuid=" + t, "NVPh5oo715z5DIWAeQlhMDsWXXQV4hwt" };
var  sign_paramStr = string.Join("", sign_params);
var signature= MD5Encryption.Encrypt(sign_paramStr);

var response =await "https://complexsearch.kugou.com/v2/search/song"
    .SetHeaders(headers)
    .SetQueries(new Dictionary<string, object> {
    {"callback","callback123"},
    {"page",1},
    {"keyword",searchKeyWord??""},
    {"pagesize","30"},
    {"bitrate","0"},
    {"isfuzzy","0"},
    {"inputtype","0"},
    {"platform","WebFilter"},
    {"userid","0"},
    {"clientver","2000"},
    {"iscorrection","1"},
    {"privilege_filter","0"},
    {"token",""},
    {"srcappid","2919"},
    {"clienttime",t},
    {"mid",t},
    {"uuid",t},
    {"dfid","-"},
    {"signature",signature}
}).GetAsStringAsync();

//返回值为JsonpCallback
var responseJson = response.Replace("callback123(", "").TrimEnd().TrimEnd(')');

var clay = Clay.Parse(responseJson);

List<dynamic> singerlist = new List<dynamic>();
Console.WriteLine("序号   FileName");

int index = 0;
foreach (var item in clay.data.lists)
{
    index++;
    singerlist.Add(item);
    Console.WriteLine($"{index.ToString().PadLeft(2,'0')}  {item.FileName}");
}

Console.WriteLine("请输入序号:");
string userInput= Console.ReadLine();
writeIndex = int.Parse(userInput);

var song_info = singerlist[writeIndex - 1];

//获取文件下载路径
var responFileInfo = await "https://wwwapi.kugou.com/yy/index.php"
    .SetHeaders(headers)
    .SetQueries(new Dictionary<string, object>
    {
        {"r", "play/getdata" },
        {"callback", "jQuery191035601158181920933_1653052693184" },
        {"hash", song_info.FileHash },
        {"dfid", "2mSZvv2GejpK2VDsgh0K7U0O" },
        {"appid", "1014" },
        {"mid", "c18aeb062e34929c6e90e3af8f7e2512" },
        {"platid", "4" },
        {"album_id", song_info.AlbumID },
        {"_", "1653050047389" }
    }).GetAsStringAsync();

var responFileInfoJson= responFileInfo.Substring(42).TrimEnd().TrimEnd(';').TrimEnd(')');
clay = Clay.Parse(responFileInfoJson);
string fileUrl = clay.data.play_url;

//下载文件
var bytes= await fileUrl.SetHeaders(headers).GetAsByteArrayAsync();

if (!Directory.Exists("./music"))
{
    Directory.CreateDirectory("./music");
}
Console.WriteLine($"{clay.data.audio_name} 下载中,请稍等....");
using (FileStream fs = new FileStream($"./music/{clay.data.audio_name}.mp3", FileMode.Create, FileAccess.Write))
{
    fs.Write(bytes, 0, bytes.Length);
}
Console.WriteLine("下载完成,按任意键退出.........");
Console.ReadKey();