using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using AngleSharp.Parser.Html;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;

namespace antiRR
{

    public class Artist
    {
        private string ArtistName;
        public string artistName
        {
            get { return this.ArtistName; }
            set
            {
                if (value != null)
                {
                    this.ArtistName = value;
                    try
                    {
                        this.getArtistId();
                    }
                    catch
                    {
                        artistId = -1;
                    }
                }
            }
        }
        public int artistId { get; set; }

        public Artist()
        {

        }

        public Artist(string artistName)
        {
            this.artistName = artistName;
        }

        public Artist(Artist artist)
        {
            this.artistName = artist.artistName;
            getArtistId();
        }

        public void getArtistId()
        {
            gateToDB gate = new gateToDB(null, null);
            artistId = gate.getArtistIdByName(this);
        }
    }

    public class Song:IDisposable
    {
        public int songId;       
        public Artist artist;
        private string SongName;
        public string songName
        {
            get { return this.SongName; }
            set
            {
                if(value!=null)
                {
                    this.SongName = value;
                    try
                    {
                        getSongId();
                    }
                    catch
                    { songId = -1; }
                }              
            }
        }
        public bool isRussianRetardedSong;

        public Song(string data)
        {
            artist = new Artist();
            isRussianRetardedSong = false;
            var t = data.Split('-');
            artist.artistName = t[0].Replace('"', ' ').Trim(' ');
            artist.artistName = t[0].Replace("&amp", "and");
            artist.artistName = t[0].Replace("&amp;", "and");
            songName = t[1].Replace('"', ' ').Trim(' ');
            songName = t[1].Replace("&amp", "and");
            songName = t[1].Replace("&amp;", "and");
            if (songName.Where((x)=> (x>='А' && x <= 'Я') || (x >= 'а' && x <= 'я')).Count()!=0)
            {
                isRussianRetardedSong = true;
            }
            if (artist.artistName.Where((x) => (x >= 'А' && x <= 'Я') || (x >= 'а' && x <= 'я')).Count() != 0)
            {
                isRussianRetardedSong = true;
            }
        }

        public Song()
        {
            artist = new Artist();
        }

        public Song Clone()
        {
            Song newSong = new Song();
            newSong.artist.artistName = this.artist.artistName;
            newSong.songName = this.songName;
            newSong.isRussianRetardedSong = this.isRussianRetardedSong;
            return newSong;
        }

        public void processSong()
        {
            isRussianRetardedSong = false;
            try
            {
                artist.artistName = artist.artistName.Replace('"', ' ').Trim(' ');
                artist.artistName = artist.artistName.Replace("&amp", "and");
                artist.artistName = artist.artistName.Replace("&amp;", "and");
                songName = songName.Replace('"', ' ').Trim(' ');
                songName = songName.Replace("&amp", "and");
                songName = songName.Replace("&amp;", "and");
                if (songName.Where((x) => (x >= 'А' && x <= 'Я') || (x >= 'а' && x <= 'я')).Count() != 0)
                {
                    isRussianRetardedSong = true;
                }
                if (artist.artistName.Where((x) => (x >= 'А' && x <= 'Я') || (x >= 'а' && x <= 'я')).Count() != 0)
                {
                    isRussianRetardedSong = true;
                }
            }
            catch {}

        }

        public override string ToString()
        {
            return artist.artistName + " - " + songName;
        }

        public static bool operator !=(Song song1,Song song2)
        {
            bool check = false;

            if(Object.ReferenceEquals(song2, null))
            {
                check = !Object.ReferenceEquals(song1, null) && Object.ReferenceEquals(song2, null) ? true : false;
                return check;
            }

            return (song1.artist.artistName != song2.artist.artistName) || (song1.songName != song2.songName) ? true : false;
        }

        public static bool operator ==(Song song1, Song song2)
        {
            bool check = false;

            if (Object.ReferenceEquals(song2, null))
            {
                check = Object.ReferenceEquals(song1, null) && Object.ReferenceEquals(song2, null) ? true : false;
                return check;
            }

            return (song1.artist.artistName != song2.artist.artistName) || (song1.songName != song2.songName) ? false : true;
        }


        private void getSongId()
        {
            gateToDB gate = new gateToDB(null,null);
            songId = gate.getSongIdByName(this);
        }
        
        public void Dispose()
        {
            artist = null;
            SongName = null;
        }
    }

    public class pageParser
    {
        string URL;
        public pageParser(string URL)
        {
            this.URL = URL;
        }

        private async Task<string> getWebPageAsync(string cookie)
        {
            HttpClient client;
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();           
            if(cookie!=null)
            {
                handler.CookieContainer.Add(new Uri(URL), new Cookie("AllTrackRadio", "415"));
                client = new HttpClient(handler);
            }
            else
            {
                client = new HttpClient();
            }
            string responseFromServer = "";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1251");
            using (Stream stream =  await client.GetStreamAsync(URL))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("windows-1251")))
                {
                    responseFromServer = reader.ReadToEnd();
                }
            }
            return responseFromServer;
        }

        private async Task<IHtmlDocument> mainParserAsync(string cookie)
        {
            var parser = new HtmlParser();
            var doc = parser.Parse(await getWebPageAsync(cookie));
            return doc;
        }

        private Song getSongForParser1Ver1(INode item)
        {
            Regex regex = new Regex("(.+-.+)");
            Match match = regex.Match(item.NodeValue);
            if (match.Success)
            {
                Group grp = match.Groups[1];
                return new Song(grp.Value);
            }
            return null;
        }

        private Song getSongForParser1Ver2(IElement item)
        {
            Regex regex = new Regex("(.+-.+<)");
            Match match = regex.Match(item.InnerHtml);
            if (match.Success)
            {
                Group grp = match.Groups[1];
                return new Song(grp.Value.Split('<')[0]);
            }
            return null;
        }

        public async Task<List<Song>> parser1Async()
        {
            IElement resPfParse;
            var parsedPage = await mainParserAsync(null);
            try
            {
                var temp = (from Page in parsedPage.All.AsParallel() where Page.ClassName == "plGroupOld" select Page).ToList();
                resPfParse = temp[0];
            }
            catch (Exception ex)
            {
                throw ex;
            }

            var arr1 = (from item in resPfParse.ChildNodes.AsParallel() where item.NodeName == "#text" select item).ToList();
            var songsInArr1 = (from item in arr1 where item.NodeValue != "" select getSongForParser1Ver1(item)).ToList();

            var arr2 = (from item in resPfParse.Children.AsParallel() where item.InnerHtml != null select item).ToList();
            var songsInArr2 = (from item in arr2.AsParallel() select getSongForParser1Ver2(item)).ToList();

            var res = (from u in songsInArr1 select u).Union(from u in songsInArr2 select u).ToList();

            return (from item in res where item != null select item).ToList();
        }

        private Song getSongForParser2(IElement item)
        {
            try
            {
                bool contains = false;
                Song tempSong = new Song();
                List<IElement> arr1 = (from a in item.Children.AsParallel() where a.ClassName == "MiddleBlack" select a).ToList();
                Parallel.ForEach(arr1, (b) =>
                {
                    try
                    {
                        tempSong.artist.artistName = (from a in b.ChildNodes.AsParallel() where a.NodeName == "#text" select a).ToList()[0].NodeValue;
                        tempSong.songName = (from a in b.Children.AsParallel() where a.NodeName == "SPAN" select a).ToList()[0].InnerHtml;
                        tempSong.processSong();
                        contains = true;
                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }                    
                });
                if (contains)
                {
                    contains = false;
                }
                else
                {
                    tempSong.artist.artistName = (from a in item.ChildNodes.AsParallel() where a.NodeName == "#text" select a).ToList()[0].NodeValue;
                    tempSong.songName = (from a in item.Children.AsParallel() where a.NodeName == "SPAN" select a).ToList()[0].InnerHtml;
                    tempSong.processSong();
                }
                return tempSong;
            }
            catch (Exception ex)
            {
                //throw ex;
                return null;
            }           
        }

        public async Task<List<Song>> parser2Async()
        {
            var parsedPage = await mainParserAsync("AllTrackRadio=415");
            var resOfParse = (from Page in parsedPage.All.AsParallel() where Page.ClassName == "track shFloat shLineNowrap" select Page).ToList();
            try
            {
                var result = (from item in resOfParse select getSongForParser2(item)).ToList();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<Song>> getSongsAsync()
        {
            List<Song> songs = new List<Song>();
            try
            {
                songs = await parser1Async();
            }
            catch
            { }
            if(songs.Count==0)
            {
                try
                {
                    songs = await parser2Async();
                }
                catch
                { }
                if (songs.Count==0)
                {
                    return null;
                }
                else
                {
                    return songs;
                }
            }
            else
            {
                return songs;
            }
        }
    }
}
