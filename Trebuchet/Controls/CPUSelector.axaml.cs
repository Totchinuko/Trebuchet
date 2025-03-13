using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TrebuchetLib;

namespace Trebuchet.Controls
{
    /// <summary>
    /// Interaction logic for CPUSelector.xaml
    /// </summary>
    public partial class CpuSelector : UserControl
    {
        // public static readonly DependencyProperty CPUAffinityProperty = DependencyProperty.Register("CPUAffinity",
        //     typeof(long),
        //     typeof(CPUSelector),
        //     new FrameworkPropertyMetadata(0L,
        //         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        //         OnCPUAffinityChanged, null, true, UpdateSourceTrigger.PropertyChanged)
        //     );

        public CpuSelector()
        {
            // int maxCPU = Environment.ProcessorCount;
            // CPUList = new List<int>(64);
            // for (int i = 0; i < 64; i++)
            //     CPUList.Add(i);
            InitializeComponent();
        }

        // /// <summary>
        // /// The CPUAffinity property is a bitflag that represents the cpu thread affinity
        // /// </summary>
        // public long CPUAffinity
        // {
        //     get
        //     {
        //         return Tools.Clamp2CPUThreads((long)GetValue(CPUAffinityProperty));
        //     }
        //     set
        //     {
        //         SetValue(CPUAffinityProperty, Tools.Clamp2CPUThreads(value));
        //     }
        // }
        //
        // public List<int> CPUList { get; }
        //
        // private static void OnCPUAffinityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        // {
        //     if (d is not CPUSelector cpuSelector) return;
        //
        //     var childs = GuiExtensions.FindVisualChildren<CheckBox>(cpuSelector.CheckboxPanel);
        //     foreach (var child in childs)
        //     {
        //         child.IsChecked = (cpuSelector.CPUAffinity & (1L << (int)child.Tag)) != 0;
        //     }
        // }
        //
        // private void CheckBox_Checked(object sender, RoutedEventArgs e)
        // {
        //     if (sender is CheckBox checkBox)
        //         CPUAffinity |= (1L << (int)checkBox.Tag);
        // }
        //
        // private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        // {
        //     if (sender is StackPanel dobject)
        //     {
        //         int index = (int)dobject.Tag;
        //         dobject.IsEnabled = index < Environment.ProcessorCount;
        //         var checkbox = GuiExtensions.FindVisualChildren<CheckBox>(dobject).FirstOrDefault();
        //         if (checkbox is not null)
        //             checkbox.IsChecked = (CPUAffinity & (1L << index)) != 0;
        //     }
        // }
        //
        // private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        // {
        //     if (sender is CheckBox checkBox)
        //         CPUAffinity &= ~(1L << (int)checkBox.Tag);
        // }
    }
}