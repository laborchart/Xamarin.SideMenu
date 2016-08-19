﻿using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace EightBot.BigBang.iOS.SideMenu
{
    public static class SideMenuManager
    {
        /* Example usage:
            // Define the menus
            SideMenuManager.menuLeftNavigationController = storyboard!.instantiateViewControllerWithIdentifier("LeftMenuNavigationController") as? UISideMenuNavigationController
            SideMenuManager.menuRightNavigationController = storyboard!.instantiateViewControllerWithIdentifier("RightMenuNavigationController") as? UISideMenuNavigationController
            // Enable gestures. The left and/or right menus must be set up above for these to work.
            // Note that these continue to work on the Navigation Controller independent of the View Controller it displays!
            SideMenuManager.menuAddPanGestureToPresent(toView: self.navigationController!.navigationBar)
            SideMenuManager.menuAddScreenEdgePanGesturesToPresent(toView: self.navigationController!.view)
        */
        public enum MenuPresentMode
        {
            MenuSlideIn, ViewSlideOut, ViewSlideInOut, MenuDissolveIn
        }

        // Bounds which has been allocated for the app on the whole device screen
        public static CGRect appScreenRect
        {
            get
            {
                var appWindowRect = UIApplication.SharedApplication.KeyWindow?.Bounds ?? new UIWindow().Bounds;
                return appWindowRect;
            }
        }

        /**
         The presentation mode of the menu.
         
         There are four modes in MenuPresentMode:
         - MenuSlideIn: Menu slides in over of the existing view.
         - ViewSlideOut: The existing view slides out to reveal the menu.
         - ViewSlideInOut: The existing view slides out while the menu slides in.
         - MenuDissolveIn: The menu dissolves in over the existing view controller.
         */
        public static MenuPresentMode menuPresentMode = MenuPresentMode.ViewSlideOut;

        /// Prevents the same view controller (or a view controller of the same class) from being pushed more than once. Defaults to true.
        public static bool menuAllowPushOfSameClassTwice = true;

        /// Pops to any view controller already in the navigation stack instead of the view controller being pushed if they share the same class. Defaults to false.
        public static bool menuAllowPopIfPossible = false;

        /// Width of the menu when presented on screen, showing the existing view controller in the remaining space. Default is 75% of the screen width.
        public static double menuWidth
        {
            get
            {
                return Math.Max(Math.Round(Math.Min((appScreenRect.Width), (appScreenRect.Height)) * 0.75), 240);
            }
        }

        /// Duration of the animation when the menu is presented without gestures. Default is 0.35 seconds.
        public static double menuAnimationPresentDuration = 0.35;

        /// Duration of the animation when the menu is dismissed without gestures. Default is 0.35 seconds.
        public static double menuAnimationDismissDuration = 0.35;

        /// Amount to fade the existing view controller when the menu is presented. Default is 0 for no fade. Set to 1 to fade completely.
        public static double menuAnimationFadeStrength = 0;

        /// The amount to scale the existing view controller or the menu view controller depending on the `menuPresentMode`. Default is 1 for no scaling. Less than 1 will shrink, greater than 1 will grow.
        public static double menuAnimationTransformScaleFactor = 1;

        /// The background color behind menu animations. Depending on the animation settings this may not be visible. If `menuFadeStatusBar` is true, this color is used to fade it. Default is black.
        public static UIColor menuAnimationBackgroundColor;

        /// The shadow opacity around the menu view controller or existing view controller depending on the `menuPresentMode`. Default is 0.5 for 50% opacity.
        public static double menuShadowOpacity = 0.5;

        /// The shadow color around the menu view controller or existing view controller depending on the `menuPresentMode`. Default is black.
        public static UIColor menuShadowColor = UIColor.Black;

        /// The radius of the shadow around the menu view controller or existing view controller depending on the `menuPresentMode`. Default is 5.
        public static double menuShadowRadius = 5;

        /// The left menu swipe to dismiss gesture.
        public static UIPanGestureRecognizer menuLeftSwipeToDismissGesture;

        /// The right menu swipe to dismiss gesture.
        public static UIPanGestureRecognizer menuRightSwipeToDismissGesture;

        /// The strength of the parallax effect on the existing view controller. Does not apply to `menuPresentMode` when set to `ViewSlideOut`. Default is 0.
        public static int menuParallaxStrength = 0;

        /// Draws the `menuAnimationBackgroundColor` behind the status bar. Default is true.
        public static bool menuFadeStatusBar = true;

        /// - Warning: Deprecated. Use `menuAnimationTransformScaleFactor` instead.
        public static double menuAnimationShrinkStrength
        {
            get
            {
                return menuAnimationTransformScaleFactor;
            }
            set
            {
                menuAnimationTransformScaleFactor = value;
            }
        }

        /**
         The blur effect style of the menu if the menu's root view controller is a UITableViewController or UICollectionViewController.
         
         - Note: If you want cells in a UITableViewController menu to show vibrancy, make them a subclass of UITableViewVibrantCell.
         */
        private static UIBlurEffectStyle _menuBlurEffectStyle;
        public static UIBlurEffectStyle menuBlurEffectStyle {
            get { return _menuBlurEffectStyle; }
            set {
                if (value != _menuBlurEffectStyle) {
                    _menuBlurEffectStyle = value;
                    updateMenuBlurIfNecessary();
                }
            }
        }

        /// The left menu.
        private static UISideMenuNavigationController _menuLeftNavigationController;
        public static UISideMenuNavigationController menuLeftNavigationController
        {
            get { return _menuLeftNavigationController; }
            set
            {
                if (_menuLeftNavigationController?.PresentingViewController == null)
                {
                    removeMenuBlurForMenu(_menuLeftNavigationController);

                    _menuLeftNavigationController = value;

                    setupNavigationController(_menuLeftNavigationController, true);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SideMenu Warning: menuLeftNavigationController cannot be modified while it's presented.");
                    return;
                }
            }
        }

        /// The right menu.
        private static UISideMenuNavigationController _menuRightNavigationController;
        public static UISideMenuNavigationController menuRightNavigationController
        {
            get { return _menuRightNavigationController; }
            set
            {
                if (_menuRightNavigationController?.PresentingViewController == null)
                {
                    removeMenuBlurForMenu(_menuRightNavigationController);

                    _menuRightNavigationController = value;

                    setupNavigationController(_menuRightNavigationController, false);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SideMenu Warning: menuRightNavigationController cannot be modified while it's presented.");
                    return;
                }
            }
        }

        private static void setupNavigationController(UISideMenuNavigationController forMenu, bool leftSide)
        {
            if (forMenu == null)
                return;

            var exitPanGesture = new UIPanGestureRecognizer();
            exitPanGesture.AddTarget(/*SideMenuTransition.self, */ () => SideMenuTransition.Current.handleHideMenuPan(exitPanGesture));
            forMenu.View.AddGestureRecognizer(exitPanGesture);
            forMenu.TransitioningDelegate = SideMenuTransition.Current.TransitioningDelegate;
            forMenu.ModalPresentationStyle = UIModalPresentationStyle.OverFullScreen;
            forMenu.LeftSide = leftSide;
            if (leftSide)
            {
                menuLeftSwipeToDismissGesture = exitPanGesture;
            }
            else
            {
                menuRightSwipeToDismissGesture = exitPanGesture;
            }
            updateMenuBlurIfNecessary();
        }

        private static void updateMenuBlurIfNecessary()
        {
            if (menuLeftNavigationController != null)
                setupMenuBlurForMenu(menuLeftNavigationController);

            if (menuRightNavigationController != null)
                setupMenuBlurForMenu(menuRightNavigationController);
        }

        private static void setupMenuBlurForMenu(UISideMenuNavigationController forMenu)
        {
            removeMenuBlurForMenu(forMenu);

            var view = forMenu.VisibleViewController?.View;

            if (forMenu == null ||
                view == null ||
                UIKit.UIAccessibility.IsReduceTransparencyEnabled)
            {
                return;
            }

            if (forMenu.OriginalMenuBackgroundColor == null)
            {
                forMenu.OriginalMenuBackgroundColor = view.BackgroundColor;
            }

            var blurEffect = UIBlurEffect.FromStyle(menuBlurEffectStyle);
            var blurView = new UIVisualEffectView(blurEffect);
            view.BackgroundColor = UIColor.Clear;
            var tableViewController = forMenu.VisibleViewController as UITableViewController;
            if (tableViewController != null)
            {
                tableViewController.TableView.BackgroundView = blurView;
                tableViewController.TableView.SeparatorEffect = UIVibrancyEffect.FromBlurEffect(blurEffect);
                tableViewController.TableView.ReloadData();
            }
            else
            {
                blurView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
                blurView.Frame = view.Bounds;
                view.InsertSubview(blurView, atIndex: 0);
            }
        }

        private static void removeMenuBlurForMenu(UISideMenuNavigationController forMenu)
        {
            if (forMenu == null)
                return;

            var originalMenuBackgroundColor = forMenu.OriginalMenuBackgroundColor;
            var view = forMenu.VisibleViewController?.View;
            if (forMenu == null ||
                    originalMenuBackgroundColor == null ||
                    view == null)
            {
                return;
            }

            view.BackgroundColor = originalMenuBackgroundColor;
            forMenu.OriginalMenuBackgroundColor = null;

            var tableViewController = forMenu.VisibleViewController as UITableViewController;
            var blurView = view.Subviews != null && view.Subviews.Length > 0 ? view.Subviews[0] as UIVisualEffectView : null;
            if (tableViewController != null)
            {
                tableViewController.TableView.BackgroundView = null;
                tableViewController.TableView.SeparatorEffect = null;
                tableViewController.TableView.ReloadData();
            }
            else if (blurView != null)
            {
                blurView.RemoveFromSuperview();
            }
        }

        /**
         Adds screen edge gestures to a view to present a menu.
         
         - Parameter toView: The view to add gestures to.
         - Parameter forMenu: The menu (left or right) you want to add a gesture for. If unspecified, gestures will be added for both sides.

         - Returns: The array of screen edge gestures added to `toView`.
         */
        public static List<UIScreenEdgePanGestureRecognizer> menuAddScreenEdgePanGesturesToPresent(UIView toView, UIRectEdge? forMenu = null) {
            var gestures = new List<UIScreenEdgePanGestureRecognizer>();

            if (forMenu != UIRectEdge.Right)
            {
                var leftScreenEdgeGestureRecognizer = new UIScreenEdgePanGestureRecognizer();
                leftScreenEdgeGestureRecognizer.AddTarget(/*SideMenuTransition.Current, */ (_) => SideMenuTransition.Current.handlePresentMenuLeftScreenEdge(leftScreenEdgeGestureRecognizer));
                leftScreenEdgeGestureRecognizer.Edges = UIRectEdge.Left;
                leftScreenEdgeGestureRecognizer.CancelsTouchesInView = true;
                toView.AddGestureRecognizer(leftScreenEdgeGestureRecognizer);
                gestures.Add(leftScreenEdgeGestureRecognizer);
            }

            if (forMenu != UIRectEdge.Left)
            {
                var rightScreenEdgeGestureRecognizer = new UIScreenEdgePanGestureRecognizer();
                rightScreenEdgeGestureRecognizer.AddTarget(/*SideMenuTransition.Current, */ (_) => SideMenuTransition.Current.handlePresentMenuRightScreenEdge(rightScreenEdgeGestureRecognizer));
                rightScreenEdgeGestureRecognizer.Edges = UIRectEdge.Right;
                rightScreenEdgeGestureRecognizer.CancelsTouchesInView = true;
                toView.AddGestureRecognizer(rightScreenEdgeGestureRecognizer);
                gestures.Add(rightScreenEdgeGestureRecognizer);
            }

            return gestures;
        }

        /**
         Adds a pan edge gesture to a view to present menus.
         
         - Parameter toView: The view to add a pan gesture to.
         
         - Returns: The pan gesture added to `toView`.
         */
        public static UIPanGestureRecognizer menuAddPanGestureToPresent(UIView toView)
        {
            var panGestureRecognizer = new UIPanGestureRecognizer();
            panGestureRecognizer.AddTarget(/*SideMenuTransition.self, */() => SideMenuTransition.Current.handlePresentMenuPan(panGestureRecognizer));
            toView.AddGestureRecognizer(panGestureRecognizer);

            return panGestureRecognizer;
        }
    }
}