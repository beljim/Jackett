﻿using Jackett.Utils.Clients;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Jackett.Indexers;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Jackett;
using Newtonsoft.Json;

namespace JackettTest.Indexers
{
    [TestFixture]
    class BakaBTTests : TestBase
    {
        [Test]
        public async void should_return_be_able_to_login_successfully()
        {
            // Do Login
            TestUtil.RegisterStringCall(new WebRequest()
            {
                Url = "http://bakabt.me/login.php",
                Cookies = "bbtid=b",
                Type = RequestType.POST,
                Referer = "http://bakabt.me/",
                PostData = new Dictionary<string, string>()
                {
                     {"username", "user" },
                     {"password", "pwd" },
                     {"returnto", "/index.php" }
                 }
            }, (req) => {
                return new WebClientStringResult()
                {
                    Status = System.Net.HttpStatusCode.Found,
                    Cookies = "bbtid=c",
                };
            });

            // Get login form
            TestUtil.RegisterStringCall(new WebRequest()
            {
                Url = "http://bakabt.me/login.php",
                Type = RequestType.GET
            }, (req) => {
                return new WebClientStringResult()
                {
                    Cookies = "bbtid=b",
                    Status = System.Net.HttpStatusCode.Found
                };
            });

            // Get logged in page
            TestUtil.RegisterStringCall(new WebRequest()
            {
                Cookies = "bbtid=c",
                Type = RequestType.GET,
                Url = "http://bakabt.me/browse.php?only=0&hentai=1&incomplete=1&lossless=1&hd=1&multiaudio=1&bonus=1&c1=1&reorder=1&q="
            }, (req) => {
                return new WebClientStringResult()
                {
                    Content = TestUtil.GetResource("Indexers/BakaBTTestsSearchPage.html"),
                    Status = System.Net.HttpStatusCode.OK
                };
            });

            var indexer = TestUtil.Container.ResolveNamed<IIndexer>(BakaBT.GetIndexerID(typeof(BakaBT))) as BakaBT;

            indexer.DisplayName.Should().Be("BakaBT");
            indexer.DisplayDescription.Should().Be("Anime Community");
            indexer.ID.Should().Be("bakabt");

            indexer.LoginUrl.Should().Be("http://bakabt.me/login.php");

            var token = JObject.Parse("{\"username\":\"user\",\"password\":\"pwd\"}");
            await indexer.ApplyConfiguration(token);
            indexer.IsConfigured.Should().Be(true);

            ((string)TestUtil.IndexManager.LastSavedConfig["cookies"]).Should().Be("bbtid=c");
        }

        [Test]
        public async void should_return_be_able_to_login_unsuccessfully()
        {
            // Do Login
            TestUtil.RegisterStringCall(new WebRequest()
            {
                Url = "http://bakabt.me/login.php",
                Cookies = "bbtid=b",
                Type = RequestType.POST,
                Referer = "http://bakabt.me/",
                PostData = new Dictionary<string, string>()
                {
                     {"username", "user" },
                     {"password", "pwd" },
                     {"returnto", "/index.php" }
                 }
            }, (req) => {
                return new WebClientStringResult()
                {
                    Status = System.Net.HttpStatusCode.OK,
                    Cookies = "bbtid=c",
                    Content = TestUtil.GetResource("Indexers/BakaBTTestsLoginError.html"),
                };
            });

            // Get login form
            TestUtil.RegisterStringCall(new WebRequest()
            {
                Url = "http://bakabt.me/login.php",
                Type = RequestType.GET
            }, (req) => {
                return new WebClientStringResult()
                {
                    Cookies = "bbtid=b",
                    Status = System.Net.HttpStatusCode.Found
                };
            });

            var indexer = TestUtil.Container.ResolveNamed<IIndexer>(BakaBT.GetIndexerID(typeof(BakaBT))) as BakaBT;

            var token = JObject.Parse("{\"username\":\"user\",\"password\":\"pwd\"}");
            try {
                await indexer.ApplyConfiguration(token);
            }
            catch(ExceptionWithConfigData e)
            {
                e.Message.Should().Be("Username or password is incorrect");
            }

            indexer.IsConfigured.Should().Be(false);
        }

        [Test]
        public async void should_return_be_able_to_scrape_the_search_page()
        {
            // Do Search
            TestUtil.RegisterStringCall(new WebRequest()
            {
                Url = "http://bakabt.me/browse.php?only=0&hentai=1&incomplete=1&lossless=1&hd=1&multiaudio=1&bonus=1&c1=1&reorder=1&q=Series",
                Cookies = "bbtid=c",
                Type = RequestType.GET
            }, (req) => {
                return new WebClientStringResult()
                {
                    Status = System.Net.HttpStatusCode.OK,
                    Cookies = "bbtid=c",
                    Content = TestUtil.GetResource("Indexers/BakaBTTestsSearchPage.html"),
                };
            });

            var indexer = TestUtil.Container.ResolveNamed<IIndexer>(BakaBT.GetIndexerID(typeof(BakaBT))) as BakaBT;

            indexer.LoadFromSavedConfiguration(JObject.Parse("{\"cookies\":\"bbtid=c\"}"));
            var results = await indexer.PerformQuery(new Jackett.Models.TorznabQuery() { SearchTerm = "Series S1", Season = 1 });

            results.Count().Should().Be(44);
            results.First().Title.Should().Be("Golden Time Season 1 (BD 720p) [FFF]");
            results.First().Guid.Should().Be("http://bakabt.me/torrent/180302/golden-time-bd-720p-fff");
            results.First().Comments.Should().Be("http://bakabt.me/torrent/180302/golden-time-bd-720p-fff");
            results.First().Size.Should().Be(10307921920);
            results.First().Description.Should().Be("Golden Time Season 1 (BD 720p) [FFF]");
            results.First().Link.Should().Be("http://bakabt.me/torrent/180302/golden-time-bd-720p-fff");
            results.First().Peers.Should().Be(161);
            results.First().Seeders.Should().Be(151);
            results.First().MinimumRatio.Should().Be(1);

            results.ElementAt(1).Title.Should().Be("Yowamushi Pedal Season 1 (BD 720p) [Commie]");
            results.ElementAt(4).Title.Should().Be("Dungeon ni Deai o Motomeru no wa Machigatte Iru Darouka: Familia Myth Season 1 (480p) [HorribleSubs]");
            results.ElementAt(5).Title.Should().Be("Is It Wrong to Try to Pick Up Girls in a Dungeon? Season 1 (480p) [HorribleSubs]");
        }
    }
}
