using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Recive_Request.Classes
{
   public static class StaticMethods
    {

    
       public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
       {
           if (depObj != null)
           {
               for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
               {
                   DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                   if (child != null && child is T)
                   {
                       yield return (T)child;
                   }

                   foreach (T childOfChild in FindVisualChildren<T>(child))
                   {
                       yield return childOfChild;
                   }
               }
           }


       }
    
     
           public static void RemoveChild(this DependencyObject parent, UIElement child)
           {
               var panel = parent as Panel;
               if (panel != null)
               {
                   panel.Children.Remove(child);
                   return;
               }

               var decorator = parent as Decorator;
               if (decorator != null)
               {
                   if (decorator.Child == child)
                   {
                       decorator.Child = null;
                   }
                   return;
               }

               var contentPresenter = parent as ContentPresenter;
               if (contentPresenter != null)
               {
                   if (contentPresenter.Content == child)
                   {
                       contentPresenter.Content = null;
                   }
                   return;
               }

               var contentControl = parent as ContentControl;
               if (contentControl != null)
               {
                   if (contentControl.Content == child)
                   {
                       contentControl.Content = null;
                   }
                   return;
               }

               // maybe more
           }
       }
    
}
