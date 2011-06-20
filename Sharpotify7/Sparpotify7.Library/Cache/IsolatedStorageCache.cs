using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;

namespace Sharpotify.Cache
{
    public class IsolatedStorageCache : ICache
    {
            #region Constants
            private const string CACHE_DIRECTORY_NAME = @"cache";
            #endregion

            #region Fields

            private IsolatedStorageFile isoStorage;
            private string directory;
            #endregion

            #region Factory

            /// <summary>
            /// Create a new <see cref="FileCache"/> with a default directory.
            /// The directory will be the value of the jotify.cache system
            /// property or './cache' if that property is
            /// undefined.
            /// </summary>
            public IsolatedStorageCache() : this(CACHE_DIRECTORY_NAME)
            {
            }

            public IsolatedStorageCache(string directory)
            {
                this.isoStorage = IsolatedStorageFile.GetUserStoreForApplication();
                this.directory = directory;
            }

            #endregion

            #region methods

            private void ClearDirectory(string folder, bool includeSubDirs)
            {
               try
               {
                   if (this.isoStorage.DirectoryExists(folder))
                   {
                       var files = this.isoStorage.GetFileNames(folder);
                       foreach (var file in files)
                       {
                           this.isoStorage.DeleteFile(file);
                       }
                   }
                  
                   if (includeSubDirs)
                   {
                       var folders = this.isoStorage.GetDirectoryNames(folder);
                       foreach (var di in folders)
                       {
                           this.ClearDirectory(di, true);
                       }
                   }
               }
               catch (Exception ex)
               {

               }
            }

            private string GetFullPath(string category, string hash)
            {
                return Path.Combine(Path.Combine(directory, category), hash);
            }

            #endregion


            #region Cache members

            public void Clear()
            {
                ClearDirectory(directory, true);
            }

            public void Clear(string category)
            {
                ClearDirectory(Path.Combine(directory, category), false);
            }

            public bool Contains(string category, string hash)
            {
                return this.isoStorage.FileExists(GetFullPath(category, hash));
            }

            public byte[] Load(string category, string hash)
            {
                try
                {
                    using (IsolatedStorageFileStream fs = this.isoStorage.OpenFile(GetFullPath(category, hash), FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        return buffer;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public void Remove(string category, string hash)
            {
                try
                {
                     this.isoStorage.DeleteFile(GetFullPath(category, hash));
                }
                catch (Exception)
                {
                    throw; //FIXME
                }
            }

            public void Store(string category, string hash, byte[] data)
            {
                Store(category, hash, data, data.Length);
            }

            private void EnsureDirectories(string fileName)
            {
                string dir = Path.GetDirectoryName(fileName);
                string[] folders = dir.Split(@"\".ToCharArray());
                string currentFolder = string.Empty;
                foreach (var f in folders)
                {
                    if (!string.IsNullOrEmpty(currentFolder))
                    {
                        currentFolder += @"\";
                    }

                    currentFolder += f;
                    EnsureDirectory(currentFolder);            
                }
                
            }

            private void EnsureDirectory(string folder)
            {                
                if (!this.isoStorage.DirectoryExists(folder))
                {
                    this.isoStorage.CreateDirectory(folder);
                }
            }

            public void Store(string category, string hash, byte[] data, int size)
            {
                try
                {
                    string fileName = GetFullPath(category, hash);
                    EnsureDirectories(fileName);
                    using (IsolatedStorageFileStream fs = this.isoStorage.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(data, 0, size);
                    }
                }
                catch (Exception ex)
                {
                    //Ignore errors
                }
            }

            public string[] List(string category)
            {
                List<string> fileList = new List<string>();
                try
                {
                    var files = this.isoStorage.GetFileNames(Path.Combine(this.directory, category));
                    foreach (string fi in files)
                        fileList.Add(fi);
                }
                catch (Exception)
                {
                    throw; //FIXME
                }
                return fileList.ToArray();
            }

            #endregion
    }
}
