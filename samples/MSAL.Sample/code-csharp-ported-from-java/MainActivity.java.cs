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

using Android.Views;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.Navigation;
using MSAL.Sample;

namespace MSAL.Samples
{
    public class MainActivity : AndroidX.AppCompat.App.AppCompatActivity,
             NavigationView.IOnNavigationItemSelectedListener // ,
                                                              //IOnFragmentInteractionListener
    {
        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            throw new System.NotImplementedException();
        }

        enum AppFragment
        {
            SingleAccount,
            MultipleAccount,
            B2C
        }

        private AppFragment mCurrentFragment;

        private ConstraintLayout mContentMain;

        //     // @Override
        //     protected override void OnCreate(Bundle savedInstanceState) {
        //         super.onCreate(savedInstanceState);
        //         setContentView(R.layout.activity_main);

        //         mContentMain = findViewById(Resource.Id.content_main);

        //         Toolbar toolbar = findViewById(Resource.Id.toolbar);
        //         setSupportActionBar(toolbar);
        //         DrawerLayout drawer = findViewById(Resource.Id.drawer_layout);
        //         NavigationView navigationView = findViewById(Resource.Id.nav_view);
        //         ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(
        //                 this, drawer, toolbar, R.string.navigation_drawer_open, R.string.navigation_drawer_close);
        //         drawer.addDrawerListener(toggle);
        //         toggle.syncState();
        //         navigationView.setNavigationItemSelectedListener(this);

        //         //Set default fragment
        //         navigationView.setCheckedItem(Resource.Id.nav_single_account);
        //         setCurrentFragment(AppFragment.SingleAccount);
        //     }

        // @Override
        //public override bool OnNavigationItemSelected(/* final */ Android.Views.IMenuItem item)
        //{
        /* final */
        //DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
        //drawer.AddDrawerListener(new DrawerLayout(this)
        //{
        //         // @Override
        //         public void onDrawerSlide(@NonNull View drawerView, float slideOffset) { }

        //// @Override
        //public void onDrawerOpened(@NonNull View drawerView) { }

        //// @Override
        //public void onDrawerClosed(@NonNull View drawerView)
        //{
        //    // Handle navigation view item clicks here.
        //    int id = item.getItemId();

        //    if (id == Resource.Id.nav_single_account)
        //    {
        //        SetCurrentFragment(AppFragment.SingleAccount);
        //    }

        //    if (id == Resource.Id.nav_multiple_account)
        //    {
        //        SetCurrentFragment(AppFragment.MultipleAccount);
        //    }

        //    if (id == Resource.Id.nav_b2c)
        //    {
        //        SetCurrentFragment(AppFragment.B2C);
        //    }

        //    drawer.RemoveDrawerListener(this);
        //}

        //             // @Override
        //             public void OnDrawerStateChanged(int newState) { }
        //         });

        //         drawer.CloseDrawer(GravityCompat.START);
        //         return true;
        //     }

        //     private override void SetCurrentFragment(/* final */ AppFragment newFragment){
        //         if (newFragment == mCurrentFragment) {
        //             return;
        //         }

        //         mCurrentFragment = newFragment;
        //         SetHeaderString(mCurrentFragment);
        //         DisplayFragment(mCurrentFragment);
        //     }

        //     private void SetHeaderString(/* final */ AppFragment fragment){
        //         switch (fragment) {
        //             case SingleAccount:
        //                 GetSupportActionBar().SetTitle("Single Account Mode");
        //                 return;

        //             case MultipleAccount:
        //                 getSupportActionBar().SetTitle("Multiple Account Mode");
        //                 return;

        //             case B2C:
        //                 getSupportActionBar().SetTitle("B2C Mode");
        //                 return;
        //         }
        //     }

        //     private void displayFragment(/* final */ AppFragment fragment){
        //         switch (fragment) {
        //             case SingleAccount:
        //                 AttachFragment(new SingleAccountModeFragment());
        //                 return;

        //             case MultipleAccount:
        //                 AttachFragment(new MultipleAccountModeFragment());
        //                 return;

        //             case B2C:
        //                 AttachFragment(new B2CModeFragment());
        //                 return;
        //         }
        //     }

        //     private void AttachFragment(/* final */ Fragment fragment) {
        //         GetSupportFragmentManager()
        //                 .BeginTransaction()
        //                 .SetTransitionStyle(FragmentTransaction.TRANSIT_FRAGMENT_FADE)
        //                 .Replace(mContentMain.getId(),fragment)
        //                 .Commit();
        //     }
    }

}
