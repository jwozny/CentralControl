﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Central_Control
{
    public class TextSearchFilter
    {
        public TextSearchFilter(ICollectionView filteredView, TextBox textBox)
        {
            string filterText = "";
            
            filteredView.Filter = delegate (object obj)
            {
                if (String.IsNullOrEmpty(filterText))
                {
                    return true;
                }
                
                var sb = new StringBuilder();

                foreach (var propertyInfo in
                    from p in typeof(ActiveDirectory.UserPrincipalEx).GetProperties()
                    where Equals(p.PropertyType, typeof(String))
                    select p)
                {
                    sb.AppendLine(propertyInfo.GetValue(obj, null) + " ");
                }

                string str = sb.ToString();

                if (String.IsNullOrEmpty(str))
                {
                    return false;
                }

                int index = str.IndexOf(filterText, 0, StringComparison.InvariantCultureIgnoreCase);

                return index > -1;
            };

            textBox.TextChanged += delegate
            {
                filterText = textBox.Text;
                filteredView.Refresh();
            };
        }
    }
}
