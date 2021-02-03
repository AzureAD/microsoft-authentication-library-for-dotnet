// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// mc++ 
// mc++ package com.azuresamples.msalandroidapp;
// mc++ 
// mc++ import android.os.Bundle;
// mc++ 
// mc++ import androidx.annotation.NonNull;
// mc++ import androidx.appcompat.app.ActionBarDrawerToggle;
// mc++ import androidx.appcompat.app.AppCompatActivity;
// mc++ import androidx.appcompat.widget.Toolbar;
// mc++ import androidx.constraintlayout.widget.ConstraintLayout;
// mc++ import androidx.core.view.GravityCompat;
// mc++ 
// mc++ import android.view.MenuItem;
// mc++ import android.view.View;
// mc++ 
// mc++ import androidx.drawerlayout.widget.DrawerLayout;
// mc++ import androidx.fragment.app.Fragment;
// mc++ import androidx.fragment.app.FragmentTransaction;
// mc++ 
// mc++ 
// mc++ import com.google.android.material.navigation.NavigationView;
// mc++ 

using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Fragment.App;
using Google.Android.Material.Navigation;
using MSALSample;

namespace MSALSample
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    internal class MainActivity : AndroidX.AppCompat.App.AppCompatActivity,
             NavigationView.IOnNavigationItemSelectedListener
             //, OnFragmentInteractionListener
    {
        internal enum AppFragment
        {
            SingleAccount,
            MultipleAccount,
            B2C
        }

        private AppFragment mCurrentFragment;

        private ConstraintLayout mContentMain;

        // @Override
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            mContentMain = FindViewById<ConstraintLayout>(Resource.Id.content_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(
                    this, drawer, toolbar, Resource.String.navigation_drawer_open,
                    Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();
            navigationView.SetNavigationItemSelectedListener(this);

            //Set default fragment
            navigationView.SetCheckedItem(Resource.Id.nav_single_account);
            SetCurrentFragment(AppFragment.SingleAccount);
        }

        // @Override
        public bool OnNavigationItemSelected(/* final */ Android.Views.IMenuItem item)
        {
            /* final */
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.AddDrawerListener(new DrawerLayoutListener(this, drawer, item));

            drawer.CloseDrawer(GravityCompat.Start);

            return true;
        }

        public void SetCurrentFragment(/* final */ AppFragment newFragment)
        {
            if (newFragment == mCurrentFragment)
            {
                return;
            }

            mCurrentFragment = newFragment;
            SetHeaderString(mCurrentFragment);
            DisplayFragment(mCurrentFragment);
        }

        private void SetHeaderString(/* final */ AppFragment fragment)
        {
            switch (fragment)
            {
                case AppFragment.SingleAccount:
                    SupportActionBar.Title = "Single Account Mode";
                    return;
                case AppFragment.MultipleAccount:
                    SupportActionBar.Title = "Multiple Account Mode";
                    return;

                case AppFragment.B2C:
                    SupportActionBar.Title = "B2C Mode";
                    return;
            }
        }

        private void DisplayFragment(/* final */ AppFragment fragment)
        {
            switch (fragment)
            {
                case AppFragment.SingleAccount:
                    AttachFragment(new SingleAccountModeFragment());
                    return;

                case AppFragment.MultipleAccount:
                    //AttachFragment(new MultipleAccountModeFragment());
                    return;

                case AppFragment.B2C:
                    //AttachFragment(new B2CModeFragment());
                    return;
            }
        }

        private void AttachFragment(/* final */ AndroidX.Fragment.App.Fragment fragment)
        {
            this.SupportFragmentManager
                    .BeginTransaction()
                    .SetTransitionStyle(AndroidX.Fragment.App.FragmentTransaction.TransitFragmentFade)
                    .Replace(mContentMain.Id, fragment)
                    .Commit();
        }
    }

    internal class DrawerLayoutListener : Java.Lang.Object, DrawerLayout.IDrawerListener
    {
        private MainActivity _outer_object;
        private DrawerLayout _drawer;
        private IMenuItem _item;

        public DrawerLayoutListener(MainActivity outer_object, DrawerLayout drawer, IMenuItem item)
        {
            _outer_object = outer_object;
            _drawer = drawer;
            _item = item;
        }

        //// @Override
        //public void onDrawerClosed(@NonNull View drawerView)
        public void OnDrawerClosed(View drawerView)
        {
            // Handle navigation view item clicks here.
            int id = _item.ItemId;

            if (id == Resource.Id.nav_single_account)
            {
                _outer_object.SetCurrentFragment(MainActivity.AppFragment.SingleAccount);
            }

            if (id == Resource.Id.nav_multiple_account)
            {
                _outer_object.SetCurrentFragment(MainActivity.AppFragment.MultipleAccount);
            }

            if (id == Resource.Id.nav_b2c)
            {
                _outer_object.SetCurrentFragment(MainActivity.AppFragment.B2C);
            }

            _drawer.RemoveDrawerListener(this);
        }

        // @Override
        //public void onDrawerOpened(@NonNull View drawerView) { }
        public void OnDrawerOpened(View drawerView)
        {
        }

        // @Override
        //public void onDrawerSlide(@NonNull View drawerView, float slideOffset) { }
        public void OnDrawerSlide(View drawerView, float slideOffset)
        {
        }

        // @Override
        // public void OnDrawerStateChanged(int newState) { }
        public void OnDrawerStateChanged(int newState)
        {
        }
    }
}
