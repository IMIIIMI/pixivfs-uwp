﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using PixivCS;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Provider;
using PixivFSUWP.Interfaces;
using static PixivFSUWP.Data.OverAll;
using PixivFSUWP.Data.Collections;
using PixivFSUWP.Data;
using Windows.System;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserDetailPage : Page, IGoBackFlag
    {
        UserIllustsCollection itemsSource;
        int userid = 0;
        Data.UserDetail detail;
        public UserDetailPage()
        {
            this.InitializeComponent();
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
        }

        private bool _backflag { get; set; } = false;

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }

        private void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            btnWorks.IsEnabled = true;
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(string.Format("{0}\n{1}\n" +
                "{2}：https://www.pixiv.net/member.php?id={3}\n" +
                "PixivFSUWP：pixiv://user?id={3}", GetResourceString("PixivUserPlain"), detail.Name, GetResourceString("LinkPlain"), detail.ID));
            request.Data.Properties.Title = string.Format("{0}：{1}", GetResourceString("ArtistPlain"), detail.Name);
            request.Data.Properties.Description = GetResourceString("UserShareTipPlain");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            TheMainPage?.SelectNavPlaceholder(GetResourceString("UserDetailPagePlain"));

            //治标不治本的加载逻辑……反正个人插画也不会太多,全部重载就好)
            if (e.Parameter is ValueTuple<int, bool> tuple) 
            {
                Data.OverAll.UserList?.StopLoading();
                userid = tuple.Item1;
                _ = loadContents();
                //Data.OverAll.RefreshUserList(userid.ToString()); 治 本 (需删除85行并修改Collection中的瀑布流控制逻辑)
            }
            else if(e.Parameter is int id)
            {
                userid = id;
                grdDetail.Visibility = Visibility.Collapsed;
            }
            Data.OverAll.RefreshUserList(userid.ToString()); //治 标
            Data.OverAll.UserList.ResumeLoading();
            itemsSource = OverAll.UserList;
            itemsSource.CollectionChanged += ItemsSource_CollectionChanged;
            WaterfallListView.ItemsSource = itemsSource;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            OverAll.UserList?.StopLoading();
            itemsSource = null;
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(UserDetailPage), userid);
                Data.OverAll.UserList.PauseLoading();
                TheMainPage?.UpdateNavButtonState();
            }
        }

        async Task loadContents()
        {
            var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                    .GetUserDetailAsync(userid.ToString());
            detail = Data.UserDetail.FromObject(res);
            string _getText(string input) => (input == "") ? GetResourceString("PrivatePlain") : input;
            txtUsername.Text = detail.Name;
            txtAuthor.Text = detail.Name;
            txtAccount.Text = "@" + detail.Account;
            txtAuthorAccount.Text = txtAccount.Text;
            txtWebPage.Text = (detail.WebPage == "") ? GetResourceString("PrivateOrNonePlain") : detail.WebPage;
            if (detail.Gender == "") txtGender.Text = GetResourceString("PrivatePlain");
            else txtGender.Text = (detail.Gender == "male") ? GetResourceString("MalePlain") : GetResourceString("FemalePlain");
            txtBirthday.Text = _getText(detail.BirthDay);
            txtRegion.Text = _getText(detail.Region);
            txtJob.Text = _getText(detail.Job);
            string _getHW(string input) => (input == "") ? GetResourceString("UnknownPlain") : input;
            txtPC.Text = _getHW(detail.PC);
            txtMonitor.Text = _getHW(detail.Monitor);
            txtTool.Text = _getHW(detail.Tool);
            txtScanner.Text = _getHW(detail.Scanner);
            txtTablet.Text = _getHW(detail.Tablet);
            txtMouse.Text = _getHW(detail.Mouse);
            txtPrinter.Text = _getHW(detail.Printer);
            txtDesktop.Text = _getHW(detail.Desktop);
            txtMusic.Text = _getHW(detail.Music);
            txtDesk.Text = _getHW(detail.Desk);
            txtChair.Text = _getHW(detail.Chair);
            txtBtnFollow.Text = detail.IsFollowed ? GetResourceString("FollowingPlain") : GetResourceString("NotFollowingPlain");
            btnFollow.IsChecked = detail.IsFollowed;
            imgAvatar.ImageSource = await Data.OverAll.LoadImageAsync(detail.AvatarUrl);
            imgAuthor.ImageSource = imgAvatar.ImageSource;
        }

        private void WaterfallContent_Loaded(object sender, RoutedEventArgs e)
        {
            if (ActualWidth < 700) (sender as Controls.WaterfallContentPanel).Colums = 3;
            else if (ActualWidth < 900) (sender as Controls.WaterfallContentPanel).Colums = 4;
            else if (ActualWidth < 1100) (sender as Controls.WaterfallContentPanel).Colums = 5;
            else (sender as Controls.WaterfallContentPanel).Colums = 6;
        }

        private void WaterfallListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(IllustDetailPage),
                (e.ClickedItem as ViewModels
                .WaterfallItemViewModel).ItemId);
        }

        private async void BtnWorks_Click(object sender, RoutedEventArgs e)
        {
            grdUserButton.Visibility = Visibility.Visible;
            storyFade.Begin();
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            grdDetail.Visibility = Visibility.Collapsed;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            grdDetail.Visibility = Visibility.Visible;
            storyShow.Begin();
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            grdUserButton.Visibility = Visibility.Collapsed;
        }

        private async void BtnFollow_Click(object sender, RoutedEventArgs e)
        {
            var btnSender = sender as ToggleButton;
            btnSender.IsEnabled = false;
            if (btnSender.IsChecked == true)
            {
                btnSender.IsChecked = false;
                //进行关注
                txtBtnFollow.Text = GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .PostUserFollowAddAsync(detail.ID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = true;
                    txtBtnFollow.Text = GetResourceString("FollowingPlain");
                }
                btnSender.IsEnabled = true;
            }
            else
            {
                btnSender.IsChecked = true;
                //取消关注
                txtBtnFollow.Text = GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .PostUserFollowDeleteAsync(detail.ID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = false;
                    txtBtnFollow.Text = GetResourceString("NotFollowingPlain");
                }
                btnSender.IsEnabled = true;
            }
        }

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        void copyToClipboard(string content)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void BtnLink_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard(string.Format("https://www.pixiv.net/member.php?id={0}", detail.ID));
            btnShareFlyout.Hide();
        }

        private void BtnAppLink_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard(string.Format("pixiv://user?id={0}", detail.ID));
            btnShareFlyout.Hide();
        }

        ViewModels.WaterfallItemViewModel tapped = null;

        private void WaterfallListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            quickStar.Text = (tapped.IsBookmarked) ?
                GetResourceString("DeleteBookmarkPlain") :
                GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        private void WaterfallListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            quickStar.Text = (tapped.IsBookmarked) ?
                GetResourceString("DeleteBookmarkPlain") :
                GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        private async void QuickStar_Click(object sender, RoutedEventArgs e)
        {
            if (tapped == null) return;
            var i = tapped;
            var title = i.Title;
            try
            {
                //用Title作标识，表明任务是否在执行
                i.Title = null;
                if (i.IsBookmarked)
                {
                    bool res;
                    try
                    {
                        await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                            .PostIllustBookmarkDeleteAsync(i.ItemId.ToString());
                        res = true;
                    }
                    catch
                    {
                        res = false;
                    }
                    i.Title = title;
                    if (res)
                    {
                        i.IsBookmarked = false;
                        i.Stars--;
                        i.NotifyChange("StarsString");
                        i.NotifyChange("IsBookmarked");
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("DeletedBookmarkPlain"), title));
                    }
                    else
                    {
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("BookmarkDeleteFailedPlain"), title));
                    }
                }
                else
                {
                    bool res;
                    try
                    {
                        await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                            .PostIllustBookmarkAddAsync(i.ItemId.ToString());
                        res = true;
                    }
                    catch
                    {
                        res = false;
                    }
                    i.Title = title;
                    if (res)
                    {
                        i.IsBookmarked = true;
                        i.Stars++;
                        i.NotifyChange("StarsString");
                        i.NotifyChange("IsBookmarked");
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkBookmarkedPlain"), title));
                    }
                    else
                    {
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkBookmarkFailedPlain"), title));
                    }
                }
            }
            finally
            {
                //确保出错时数据不被破坏
                i.Title = title;
            }
        }

        private async void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            if (tapped == null) return;
            var i = tapped;
            FileSavePicker picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add(GetResourceString("ImageFilePlain"), new List<string>() { ".png" });
            picker.SuggestedFileName = i.Title;
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                    .GetIllustDetailAsync(i.ItemId.ToString());
                var illust = Data.IllustDetail.FromObject(res);
                using (var imgstream = await Data.OverAll.DownloadImage(illust.OriginalUrls[0]))
                {
                    using (var filestream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await imgstream.CopyToAsync(filestream.AsStream());
                    }
                }
                var updateStatus = await CachedFileManager.CompleteUpdatesAsync(file);
                if (updateStatus == FileUpdateStatus.Complete)
                    await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkSavedPlain"), i.Title));
                else
                    await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkSaveFailedPlain"), i.Title));
            }
        }
    }
}
