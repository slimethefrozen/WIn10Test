using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Win10Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        const string WatchedFolderKey = "watchedFolder";
        const string WatchedExtensionKey = "watchedExtension";

        ApplicationDataContainer localSettings;
        //StorageFolder localFolder;
        StorageFolder watchedFolder;   //points to the folder we've been told to watch
        string watchedFolderToken;  //token used to access the watchedFolder

        ObservableCollection<FileEntry> watchedFiles = new ObservableCollection<FileEntry>();


        /// <summary>
        /// Fetch a value of type T from localSettings using Key, if it doesnt exist use defaultValue
        /// Uses a global var localSettings to point to our AppData - will initialize if not already set
        /// </summary>
        /// <typeparam name="T">type of object to return</typeparam>
        /// <param name="key">key for the value</param>
        /// <param name="defaultValue">default value if the key doesnt exist</param>
        /// <returns></returns>
        T getSetting<T>(string key, T defaultValue = default(T))
        {
            if (localSettings==null)
                localSettings = ApplicationData.Current.LocalSettings;

            Object value = localSettings.Values[key];
            if (value != null)
                return (T)Convert.ChangeType(value,typeof(T));
            return defaultValue;
        }

        string getPackageInfo()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            String output = String.Format(
                               "Name: \"{0}\"\n" +
                               "Version: {1}.{2}.{3}.{4}\n" +
                               "Architecture: {5}\n" +
                               "ResourceId: \"{6}\"\n" +
                               "Publisher: \"{7}\"\n" +
                               "PublisherId: \"{8}\"\n" +
                               "FullName: \"{9}\"\n" +
                               "FamilyName: \"{10}\"\n" +
                               "IsFramework: {11}",
                               packageId.Name,
                               version.Major, version.Minor, version.Build, version.Revision,
                               packageId.Architecture,
                               packageId.ResourceId,
                               packageId.Publisher,
                               packageId.PublisherId,
                               packageId.FullName,
                               packageId.FamilyName,
                               package.IsFramework);
            return output;
        }

        async void updateWatchedFolder()
        {
            if (watchedFolderToken==null || !StorageApplicationPermissions.FutureAccessList.ContainsItem(watchedFolderToken))
            {
                Debug.WriteLine("FutureAccessList does not contain watched folder");
                watchedPath.Text = "Select folder to watch...";
            }


            if (watchedFolder==null && StorageApplicationPermissions.FutureAccessList.ContainsItem(watchedFolderToken))
            {
                watchedFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(watchedFolderToken);
                watchedPath.Text = watchedFolder.Path;
                updateWatchList();
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            //ApplicationView.PreferredLaunchViewSize = new Size(600, 600);
            //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

//            localFolder = ApplicationData.Current.LocalFolder;
//            Debug.WriteLine("LocalFolder: " + localFolder);

            Debug.WriteLine("PackageInfo: " + getPackageInfo());

            watchedFolderToken = getSetting<string>(WatchedFolderKey,null);

            watchedExtension.Text = getSetting<string>(WatchedExtensionKey, ".TiVo");

            updateWatchedFolder();  //fetch the currently watched folder

       }

        async void updateWatchList()
        {
            if (watchedFolder==null)
            {
                Debug.WriteLine("watchedFolder is null!");
                return;
            }
            IReadOnlyCollection<StorageFile> fileList = await watchedFolder.GetFilesAsync();
            Debug.WriteLine("Found " + fileList.Count + " files in path: " + watchedFolder.Path);
            watchList.Items.Clear();
            watchedFiles.Clear();
            string extension = watchedExtension.Text.ToLower();
            watchList.ItemsSource = watchedFiles;
            foreach (StorageFile file in fileList)
            {
                if (file.Name.ToLower().Contains(extension))
                {
                    FileEntry fe = new FileEntry(file.Name, false);
                    watchedFiles.Add(fe);
                    //watchList.Items.Add(fe);
                }
            }
        }

        private async void browseWatch_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker openPicker = new FolderPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".tivo");

            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to the picked file
                Debug.WriteLine("Picked folder: " + folder.Path);
                watchedPath.Text = folder.Path;
                watchedFolderToken = StorageApplicationPermissions.FutureAccessList.Add(folder,  WatchedFolderKey);
                localSettings.Values[WatchedFolderKey] = watchedFolderToken;
                watchedFolder = folder;
                updateWatchList();
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        private void confirmExtension_Click(object sender, RoutedEventArgs e)
        {
            string extension = watchedExtension.Text;
            string prevValue = getSetting<string>(WatchedExtensionKey, ".tivo");
            if (extension!=prevValue)
            {
                //only do stuff if the extension has changed
                extension = extension.Trim();

                char[] invalidFileChars = Path.GetInvalidFileNameChars();
                string[] temp = extension.Split(invalidFileChars, StringSplitOptions.RemoveEmptyEntries);
                extension = String.Join("", temp);

                if (!extension.StartsWith("."))
                    extension = "." + extension;

                watchedExtension.Text = extension;
                localSettings.Values[WatchedExtensionKey] = extension;

                updateWatchList();
            }
        }

        private void CheckBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("Checkbox changed");
        }
    }
}
