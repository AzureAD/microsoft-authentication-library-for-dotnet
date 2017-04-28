
using System;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;

namespace DesktopTestApp
{
    public partial class MsalUserRefreshTokenControl : UserControl
    {
        private TokenCache _cache;
        private RefreshTokenCacheItem _item;
        public delegate void RefreshView();

        private const string GarbageRtValue = "garbage-refresh-token";

        public RefreshView RefreshViewDelegate { get; set; }

        internal MsalUserRefreshTokenControl(TokenCache cache, RefreshTokenCacheItem item) : this()
        {
            _cache = cache;
            _item = item;
            upnLabel.Text = _item.DisplayableId;
            invalidateRefreshTokenBtn.Enabled = !_item.RefreshToken.Equals(GarbageRtValue);
        }

        public MsalUserRefreshTokenControl()
        {
            InitializeComponent();
        }

        private void InvalidateRefreshTokenBtn_Click(object sender, System.EventArgs e)
        {
            _item.RefreshToken = GarbageRtValue;
            _cache.SaveRefreshTokenCacheItem(_item);
            invalidateRefreshTokenBtn.Enabled = false;
        }

        private void signOutUserOneBtn_Click(object sender, System.EventArgs e)
        {
            _cache.Remove(_item.User, new RequestContext(Guid.Empty, null));
            RefreshViewDelegate?.Invoke();
        }
    }
}
