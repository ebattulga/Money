﻿using Microsoft.AspNetCore.Components;
using Money.Components.Bootstrap;
using Money.Models.Sorting;
using Neptuo;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Money.Components
{
    public class SortButtonBase<TType> : ComponentBase
    {
        private readonly Dictionary<TType, SortDirection> defaultSortDirection = new Dictionary<TType, SortDirection>();

        [Inject]
        internal ILog<SortButtonBase<TType>> Log { get; set; }

        [Parameter]
        protected SortDescriptor<TType> Current { get; set; }

        [Parameter]
        protected Action<SortDescriptor<TType>> CurrentChanged { get; set; }

        [Parameter]
        protected Action Changed { get; set; }

        [Parameter]
        protected Size Size { get; set; } = Size.Normal;

        protected List<(string Name, TType Value)> Items { get; } = new List<(string, TType)>();
        protected string ButtonCssClass { get; private set; }

        protected override void OnInit()
        {
            base.OnInit();

            Type parameterType = typeof(TType);
            foreach (object value in Enum.GetValues(parameterType))
            {
                TType type = (TType)value;
                string text = Enum.GetName(parameterType, value);

                MemberInfo itemInfo = parameterType
                    .GetMember(text)
                    .First();

                DescriptionAttribute attribute = itemInfo.GetCustomAttribute<DescriptionAttribute>();
                if (attribute != null)
                    text = attribute.Description;

                DefaultValueAttribute defaultValue = itemInfo.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValue != null)
                    defaultSortDirection[type] = (SortDirection)defaultValue.Value;

                Items.Add((text, type));
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (Current == null)
                UpdateCurrent(Items.First().Value);

            ButtonCssClass = "btn btn-light dropdown-toggle";
            switch (Size)
            {
                case Size.Small:
                    ButtonCssClass += " btn-sm";
                    break;
                case Size.Normal:
                    break;
                case Size.Large:
                    ButtonCssClass += " btn-lg";
                    break;
                default:
                    throw Ensure.Exception.NotSupported(Size.ToString());
            }
        }

        private SortDirection GetDefaultDirection(TType type)
        {
            if (defaultSortDirection.TryGetValue(type, out SortDirection direction))
                return direction;

            return SortDirection.Ascending;
        }

        private void UpdateCurrent(TType type)
        {
            SortDirection direction = GetDefaultDirection(type);
            Log.Debug($"Default direction='{direction}'.");

            if (Current != null)
                Log.Debug($"Current: Type='{Current.Type}', Direction='{Current.Direction}'.");
            else
                Log.Debug($"Current: null.");

            Current = Current.Update(type, direction);
            Log.Debug($"New: Type='{Current.Type}', Direction='{Current.Direction}'.");

            CurrentChanged?.Invoke(Current);
        }

        protected void OnItemClick(TType type)
        {
            UpdateCurrent(type);
            Changed?.Invoke();
        }

        protected string DirectionToIcon(SortDirection sortDirection)
        {
            if (sortDirection == SortDirection.Ascending)
                return "caret-down";

            return "caret-up";
        }
    }
}
