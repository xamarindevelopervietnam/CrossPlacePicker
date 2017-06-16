using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Gms.Common;
using Android.Gms.Location.Places.UI;
using Android.Gms.Maps.Model;
using Android.Widget;
using Plugin.CrossPlacePicker.Abstractions;
using Plugin.CurrentActivity;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Plugin.CrossPlacePicker
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    [Preserve(AllMembers = true)]
    public class CrossPlacePickerImplementation : ICrossPlacePicker
    {
        private static int REQUEST_PLACE_PICKER = 1;



        private int requestId;
        private TaskCompletionSource<Places> completionSource;

        private int GetRequestId()
        {
            int id = this.requestId;
            if (this.requestId == Int32.MaxValue)
                this.requestId = 0;
            else
                this.requestId++;

            return id;
        }

        public Task<Places> Display(CoordinateBounds bounds = null)
        {
            int id = GetRequestId();
            var ntcs = new TaskCompletionSource<Places>(id);
            if (Interlocked.CompareExchange(ref this.completionSource, ntcs, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");
            var currentactivity = CrossCurrentActivity.Current.Activity;
            var intent = new Intent(currentactivity, typeof(PlacePickerActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            if(bounds!=null)
            {
                intent.PutExtra(PlacePickerActivity.ExtraNELatitude, bounds.northeast.Latitude);
                intent.PutExtra(PlacePickerActivity.ExtraNELongitude, bounds.northeast.Longitude);
                intent.PutExtra(PlacePickerActivity.ExtraSWLatitude, bounds.southwest.Latitude);
                intent.PutExtra(PlacePickerActivity.ExtraSWLongitude, bounds.southwest.Longitude);
            }
            currentactivity.StartActivity(intent);
            EventHandler<PlacePickedEventArgs> handler = null;
            handler = (s, e) =>
            {
                var tcs = Interlocked.Exchange(ref this.completionSource, null);
                PlacePickerActivity.PlacePicked -= handler;

                if (e.RequestId != id)
                    return;

                if (e.IsCanceled)
                    tcs.SetResult(null);
                else if (e.Error != null)
                    tcs.SetException(e.Error);
                else
                    tcs.SetResult(e.Places);
            };
            PlacePickerActivity.PlacePicked += handler;
            return completionSource.Task;
        }
    }
}