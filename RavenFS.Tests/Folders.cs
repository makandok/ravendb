﻿using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Raven.Abstractions.FileSystem;

namespace RavenFS.Tests
{
    public class Folders : RavenFsTestBase
	{
		[Fact]
		public void CanGetListOfFolders()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("test/abc.txt", ms).Wait();
			client.UploadAsync("test/ced.txt", ms).Wait();
			client.UploadAsync("why/abc.txt", ms).Wait();

			var strings = client.GetFoldersAsync().Result;
			Assert.Equal(new[]{"/test", "/why"}, strings);
		}

		[Fact]
		public async void WillNotGetNestedFolders()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
            await client.UploadAsync("test/c.txt", ms);
            await client.UploadAsync("test/ab/c.txt", ms);
            await client.UploadAsync("test/ce/d.txt", ms);
            await client.UploadAsync("test/ce/a/d.txt", ms);
			await client.UploadAsync("why/abc.txt", ms);

			var strings = await client.GetFoldersAsync();
            Assert.Equal(new[] { "/test", "/why" }, strings);

            strings = await client.GetFoldersAsync("test");
            Assert.Equal(new[] { "/test/ab", "/test/ce" }, strings);
		}

        [Fact]
        public async void WillWorkWithTrailingSlash()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();
            await client.UploadAsync("test/ab/c.txt", ms);
            await client.UploadAsync("test/ce/d.txt", ms);
            await client.UploadAsync("test/ce/a/d.txt", ms);

            var strings = await client.GetFoldersAsync("test/");
            Assert.Equal(new[] { "/test/ab", "/test/ce" }, strings);
        }

        [Fact]
        public async void WillUploadFileInNestedFolders()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();
            var streamWriter = new StreamWriter(ms);
            var expected = new string('a', 1024);
            streamWriter.Write(expected);
            streamWriter.Flush();
            
            ms.Position = 0;
            await client.UploadAsync("test/something.txt", ms);
            ms.Position = 0;
            await client.UploadAsync("test/ab/c.txt", ms);

            var downloadStream = new MemoryStream();
            await client.DownloadAsync("test/ab/c.txt", downloadStream);

            Assert.Equal(expected, StreamToString(downloadStream));
        }

        [Fact]
        public async void WillUploadFileInNestedFolders_InverseOrder()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();
            var streamWriter = new StreamWriter(ms);
            var expected = new string('a', 1024);
            streamWriter.Write(expected);
            streamWriter.Flush();

            ms.Position = 0;
            await client.UploadAsync("test/ab/c.txt", ms);
            ms.Position = 0;
            await client.UploadAsync("test/something.txt", ms);

            var downloadStream = new MemoryStream();
            await client.DownloadAsync("test/ab/c.txt", downloadStream);

            Assert.Equal(expected, StreamToString(downloadStream));
        }

		[Fact]
		public void WillNotGetOtherFolders()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("test/ab/c.txt", ms).Wait();
			client.UploadAsync("test/ce/d.txt", ms).Wait();
			client.UploadAsync("test/ab/a/c.txt", ms).Wait();

			var strings = client.GetFoldersAsync("/test").Result;
			Assert.Equal(new[] {"/test/ab", "/test/ce" }, strings);
	
		}


		[Fact]
		public void CanRename()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("test/abc.txt", ms).Wait();

			client.RenameAsync("test/abc.txt", "test2/abc.txt").Wait();

			client.DownloadAsync("test2/abc.txt", new MemoryStream()).Wait();// would thorw if missing
		}



		[Fact]
		public void AfterRename_OldFolderIsGoneAndWeHaveNewOne()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("test/abc.txt", ms).Wait();

			Assert.Contains("/test", client.GetFoldersAsync().Result);

			client.RenameAsync("test/abc.txt", "test2/abc.txt").Wait();

			client.DownloadAsync("test2/abc.txt", new MemoryStream()).Wait();// would thorw if missing

			Assert.DoesNotContain("/test", client.GetFoldersAsync().Result);

			Assert.Contains("/test2", client.GetFoldersAsync().Result);

		}

		[Fact]
		public async Task CanGetListOfFilesInFolder()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			await client.UploadAsync("test/abc.txt", ms);
			await client.UploadAsync("test/ced.txt", ms);
			await client.UploadAsync("why/abc.txt", ms);

			var results = await client.GetFilesFromAsync("/test");
			var strings = results.Files.Select(x => x.Name).ToArray();
			Assert.Equal(new[] { "/test/abc.txt", "/test/ced.txt" }, strings);
		}


		[Fact]
		public void CanGetListOfFilesInFolder_Sorted_Size()
		{
			var client = NewAsyncClient();
			client.UploadAsync("test/abc.txt", new MemoryStream(new byte[4])).Wait();
			client.UploadAsync("test/ced.txt", new MemoryStream(new byte[8])).Wait();

			var strings = client.GetFilesFromAsync("/test", FilesSortOptions.Size | FilesSortOptions.Desc).Result.Files.Select(x => x.Name).ToArray();
			Assert.Equal(new[] { "/test/ced.txt", "/test/abc.txt" }, strings);
		}

		[Fact]
		public void CanGetListOfFilesInFolder_Sorted_Name()
		{
			var client = NewAsyncClient();
			client.UploadAsync("test/abc.txt", new MemoryStream(new byte[4])).Wait();
			client.UploadAsync("test/ced.txt", new MemoryStream(new byte[8])).Wait();

			var strings = client.GetFilesFromAsync("/test", FilesSortOptions.Name | FilesSortOptions.Desc).Result.Files.Select(x => x.Name).ToArray();
			Assert.Equal(new[] { "/test/ced.txt", "/test/abc.txt" }, strings);
		}


		[Fact]
		public void CanGetListOfFilesInFolderInRoot()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("test/abc.txt", ms).Wait();
			client.UploadAsync("test/ced.txt", ms).Wait();
			client.UploadAsync("why/abc.txt", ms).Wait();

			var strings = client.GetFilesFromAsync("/").Result.Files.Select(x => x.Name).ToArray();
			Assert.Equal(new string[] { }, strings);
		}


		[Fact]
		public void CanGetListOfFilesInFolder2()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("test/abc.txt", ms).Wait();
			client.UploadAsync("test/ced.txt", ms).Wait();
			client.UploadAsync("why/abc.txt", ms).Wait();

			var strings = client.GetFilesFromAsync("/test").Result.Files.Select(x => x.Name).ToArray();
			Assert.Equal(new string[] { "/test/abc.txt", "/test/ced.txt" }, strings);
		}

        [Fact]
        public void CanSearchForFilesByPattern()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();

            client.UploadAsync("abc.txt", ms).Wait();
            client.UploadAsync("def.txt", ms).Wait();
            client.UploadAsync("dhi.txt", ms).Wait();

            var fileNames =
                client.GetFilesFromAsync("/", fileNameSearchPattern: "d*").Result.Files.Select(x => x.Name).ToArray();
            Assert.Equal(new string[] { "def.txt", "dhi.txt"}, fileNames);
        }

        [Fact]
        public void CanSearchForFilesByPatternBeginningWithMultiCharacterWildcard()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();

            client.UploadAsync("abc.txt", ms).Wait();
            client.UploadAsync("def.txt", ms).Wait();
            client.UploadAsync("ghi.png", ms).Wait();
            client.UploadAsync("jkl.png", ms).Wait();

            var fileNames =
                client.GetFilesFromAsync("/", fileNameSearchPattern: "*.png").Result.Files.Select(x => x.Name).ToArray();
            Assert.Equal(new string[] { "ghi.png", "jkl.png" }, fileNames);
        }

        [Fact]
        public void CanSearchForFilesByPatternBeginningWithSingleCharacterWildcard()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();

            client.UploadAsync("abc.txt", ms).Wait();
            client.UploadAsync("def.txt", ms).Wait();
            client.UploadAsync("ghi.png", ms).Wait();
            client.UploadAsync("jkl.png", ms).Wait();

            var fileNames =
                client.GetFilesFromAsync("/", fileNameSearchPattern: "?bc?txt").Result.Files.Select(x => x.Name).ToArray();
            Assert.Equal(new string[] { "abc.txt" }, fileNames);
        }

        [Fact]
        public void CanSearchForFilesByNameWithinAFolder()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();

            client.UploadAsync("test/abc.txt", ms).Wait();
            client.UploadAsync("test/def.txt", ms).Wait();

            var fileNames =
                client.GetFilesFromAsync("/test", fileNameSearchPattern: "a*").Result.Files.Select(x => x.Name).ToArray();
            Assert.Equal(new string[] { "/test/abc.txt" }, fileNames);
        }

        [Fact]
        public void CanSearchPatternContainingNoWildcardsDoesStartsWithSearchByDefault()
        {
            var client = NewAsyncClient();
            var ms = new MemoryStream();

            client.UploadAsync("abc.txt", ms).Wait();
            client.UploadAsync("def.txt", ms).Wait();

            var fileNames =
                client.GetFilesFromAsync("/", fileNameSearchPattern: "a").Result.Files.Select(x => x.Name).ToArray();
            Assert.Equal(new string[] { "abc.txt" }, fileNames);
        }

		[Fact]
		public async void CanPage()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			await client.UploadAsync("test/abc.txt", ms);
			await client.UploadAsync("test/ced.txt", ms);
			await client.UploadAsync("why/abc.txt", ms);
			await client.UploadAsync("why1/abc.txt", ms);

			var strings = await client.GetFoldersAsync(start: 1);
			Assert.Equal(new[] { "/why", "/why1" }, strings);
		}

		[Fact]
		public void CanDetectRemoval()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("test/abc.txt", ms).Wait();
			client.UploadAsync("test/ced.txt", ms).Wait();
			client.UploadAsync("why/abc.txt", ms).Wait();
			client.UploadAsync("why1/abc.txt", ms).Wait();

			client.DeleteAsync("why1/abc.txt").Wait();

			var strings = client.GetFoldersAsync().Result;
			Assert.Equal(new[] { "/test", "/why" }, strings);
		}

		[Fact]
		public void Should_not_see_already_deleted_files()
		{
			var client = NewAsyncClient();
			var ms = new MemoryStream();
			client.UploadAsync("visible.bin", ms).Wait();
			client.UploadAsync("toDelete.bin", ms).Wait();

			client.DeleteAsync("toDelete.bin").Wait();

			var fileNames =
				client.GetFilesFromAsync("/").Result.Files.Select(x => x.Name).ToArray();
			Assert.Equal(new[] { "visible.bin" }, fileNames);
		}
	}
}