# ?? INSTALLER DEMO MODE & COMMAND-LINE REDESIGN

**Created:** 2025-11-09  
**Purpose:** Unified demo mode + production workflow  
**Goal:** One `--demo` flag for all demo scenarios

---

## ?? **PROPOSED COMMAND-LINE FLAGS:**

### **PRODUCTION MODE (No Flags):**
```bash
EcommerceStarter.Installer.exe

# Auto-detects existing installation
# Shows MaintenanceModePage if found
# Shows Fresh Install Wizard if not found
```

### **DEMO MODE (Single Flag):**
```bash
EcommerceStarter.Installer.exe --demo

# Shows Demo Mode Selection Screen
# User picks scenario to demonstrate
# All operations use mock data (no real changes)
```

### **SPECIFIC DEMO SCENARIOS (Optional):**
```bash
# Skip demo selection, go straight to scenario:
EcommerceStarter.Installer.exe --demo-fresh      # Fresh install demo
EcommerceStarter.Installer.exe --demo-upgrade    # Upgrade demo
EcommerceStarter.Installer.exe --demo-reconfig   # Reconfigure demo
EcommerceStarter.Installer.exe --demo-repair     # Repair demo
EcommerceStarter.Installer.exe --demo-uninstall  # Uninstall demo
```

### **UTILITY FLAGS:**
```bash
EcommerceStarter.Installer.exe --uninstall       # Direct to uninstall (production)
EcommerceStarter.Installer.exe --help            # Show help
EcommerceStarter.Installer.exe --version         # Show version
```

---

## ?? **IMPLEMENTATION:**

### **Step 1: Update App.xaml.cs**

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    // Parse command-line arguments
    var args = e.Args;
    
    // Check for help/version flags
    if (args.Contains("--help") || args.Contains("-h"))
    {
        ShowHelp();
        Current.Shutdown();
        return;
    }
    
    if (args.Contains("--version") || args.Contains("-v"))
    {
        ShowVersion();
        Current.Shutdown();
        return;
    }
    
    // Check for uninstall mode
    if (args.Contains("--uninstall") || args.Contains("-u"))
    {
        IsUninstallMode = true;
        var uninstallWindow = new UninstallWindow();
        uninstallWindow.Show();
        return;
    }
    
    // Check for demo mode
    if (args.Contains("--demo"))
    {
        // General demo mode - show selection screen
        IsDemoMode = true;
        DemoScenario = DetermineDemo scenario(args);
        
        if (DemoScenario == DemoScenario.Selection)
        {
            // Show demo mode selection screen
            var demoSelectionWindow = new DemoSelectionWindow();
            demoSelectionWindow.Show();
        }
        else
        {
            // Direct to specific demo scenario
            var mainWindow = new MainWindow();
            mainWindow.LaunchDemoScenario(DemoScenario);
            mainWindow.Show();
        }
        return;
    }
    
    // Production mode - normal flow
    IsDemoMode = false;
    IsDebugMode = false;
    
    var productionWindow = new MainWindow();
    productionWindow.Show();
}

private DemoScenario DetermineDemoScenario(string[] args)
{
    if (args.Contains("--demo-fresh")) return DemoScenario.FreshInstall;
    if (args.Contains("--demo-upgrade")) return DemoScenario.Upgrade;
    if (args.Contains("--demo-reconfig")) return DemoScenario.Reconfigure;
    if (args.Contains("--demo-repair")) return DemoScenario.Repair;
    if (args.Contains("--demo-uninstall")) return DemoScenario.Uninstall;
    
    return DemoScenario.Selection; // Show selection screen
}

private void ShowHelp()
{
    var helpText = @"
?????????????????????????????????????????????????????????????
?  EcommerceStarter Installer - Command-Line Options       ?
?????????????????????????????????????????????????????????????

PRODUCTION MODE:
  (no flags)              Launch installer normally
                          - Auto-detects existing installation
                          - Shows maintenance or install wizard

DEMO MODE:
  --demo                  Show demo mode selection screen
  --demo-fresh            Demo: Fresh installation
  --demo-upgrade          Demo: Upgrade existing
  --demo-reconfig         Demo: Reconfigure settings
  --demo-repair           Demo: Repair installation
  --demo-uninstall        Demo: Uninstall

UTILITY:
  --uninstall, -u         Launch uninstaller directly
  --help, -h              Show this help
  --version, -v           Show version info

EXAMPLES:
  EcommerceStarter.Installer.exe
    ? Normal installation flow

  EcommerceStarter.Installer.exe --demo
    ? Show all demo scenarios (safe, no changes)

  EcommerceStarter.Installer.exe --demo-upgrade
    ? Directly demonstrate upgrade workflow

  EcommerceStarter.Installer.exe --uninstall
    ? Launch uninstaller

For more information, visit:
https://github.com/yourusername/EcommerceStarter
";
    
    MessageBox.Show(helpText, "EcommerceStarter Installer Help", 
        MessageBoxButton.OK, MessageBoxImage.Information);
}

private void ShowVersion()
{
    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
    var versionText = $@"
EcommerceStarter Installer
Version: {version}
Build Date: {File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location):yyyy-MM-dd}
.NET: {Environment.Version}
";
    
    MessageBox.Show(versionText, "Version Information",
        MessageBoxButton.OK, MessageBoxImage.Information);
}
```

---

### **Step 2: Create DemoSelectionWindow.xaml**

```xaml
<Window x:Class="EcommerceStarter.Installer.DemoSelectionWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Demo Mode - Select Scenario" 
        Height="600" 
        Width="800"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize">
    
    <Grid Margin="40">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <StackPanel Grid.Row="0" Margin="0,0,0,30">
            <TextBlock Text="?? DEMO MODE" 
                      FontSize="32" 
                      FontWeight="Bold" 
                      HorizontalAlignment="Center"/>
            <TextBlock Text="Select a scenario to demonstrate (safe mode - no actual changes)" 
                      FontSize="14" 
                      Foreground="#666666"
                      HorizontalAlignment="Center"
                      Margin="0,10,0,0"/>
        </StackPanel>
        
        <!-- Warning Banner -->
        <Border Grid.Row="1" 
                Background="#FFF3CD" 
                BorderBrush="#FFC107"
                BorderThickness="2"
                CornerRadius="6"
                Padding="20"
                Margin="0,0,0,30">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="??" FontSize="24" VerticalAlignment="Center" Margin="0,0,15,0"/>
                <TextBlock TextWrapping="Wrap">
                    <Run Text="DEMO MODE ACTIVE:" FontWeight="Bold"/>
                    <Run Text="All operations use mock data. No actual installations, databases, or files will be modified."/>
                </TextBlock>
            </StackPanel>
        </Border>
        
        <!-- Scenario Selection -->
        <StackPanel Grid.Row="2">
            <TextBlock Text="Choose a demo scenario:" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,20"/>
            
            <!-- Fresh Install Demo -->
            <Button x:Name="FreshInstallButton" 
                    Height="70" 
                    Click="FreshInstall_Click"
                    Margin="0,0,0,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="??" FontSize="32" VerticalAlignment="Center" Margin="0,0,20,0"/>
                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="Fresh Installation" FontSize="16" FontWeight="Bold"/>
                        <TextBlock Text="Demo: Installing EcommerceStarter from scratch" 
                                  FontSize="12" 
                                  Foreground="#666666"
                                  Margin="0,4,0,0"/>
                    </StackPanel>
                </Grid>
            </Button>
            
            <!-- Upgrade Demo -->
            <Button x:Name="UpgradeButton" 
                    Height="70" 
                    Click="Upgrade_Click"
                    Margin="0,0,0,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="??" FontSize="32" VerticalAlignment="Center" Margin="0,0,20,0"/>
                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="Upgrade Existing Installation" FontSize="16" FontWeight="Bold"/>
                        <TextBlock Text="Demo: Upgrading an existing store to latest version" 
                                  FontSize="12" 
                                  Foreground="#666666"
                                  Margin="0,4,0,0"/>
                    </StackPanel>
                </Grid>
            </Button>
            
            <!-- Reconfigure Demo -->
            <Button x:Name="ReconfigureButton" 
                    Height="70" 
                    Click="Reconfigure_Click"
                    Margin="0,0,0,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="??" FontSize="32" VerticalAlignment="Center" Margin="0,0,20,0"/>
                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="Reconfigure Settings" FontSize="16" FontWeight="Bold"/>
                        <TextBlock Text="Demo: Reset admin password and update configuration" 
                                  FontSize="12" 
                                  Foreground="#666666"
                                  Margin="0,4,0,0"/>
                    </StackPanel>
                </Grid>
            </Button>
            
            <!-- Repair Demo -->
            <Button x:Name="RepairButton" 
                    Height="70" 
                    Click="Repair_Click"
                    Margin="0,0,0,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="??" FontSize="32" VerticalAlignment="Center" Margin="0,0,20,0"/>
                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="Repair Installation" FontSize="16" FontWeight="Bold"/>
                        <TextBlock Text="Demo: Fix broken files and verify installation" 
                                  FontSize="12" 
                                  Foreground="#666666"
                                  Margin="0,4,0,0"/>
                    </StackPanel>
                </Grid>
            </Button>
            
            <!-- Uninstall Demo -->
            <Button x:Name="UninstallButton" 
                    Height="70" 
                    Click="Uninstall_Click">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="???" FontSize="32" VerticalAlignment="Center" Margin="0,0,20,0"/>
                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="Uninstall" FontSize="16" FontWeight="Bold"/>
                        <TextBlock Text="Demo: Remove an existing installation" 
                                  FontSize="12" 
                                  Foreground="#666666"
                                  Margin="0,4,0,0"/>
                    </StackPanel>
                </Grid>
            </Button>
        </StackPanel>
        
        <!-- Exit Button -->
        <Button Grid.Row="3" 
                Content="Exit Demo Mode" 
                Width="150" 
                Height="40" 
                HorizontalAlignment="Right" 
                Margin="0,30,0,0"
                Click="Exit_Click"/>
    </Grid>
</Window>
```

---

### **Step 3: Create DemoScenario Enum**

```csharp
namespace EcommerceStarter.Installer.Models;

public enum DemoScenario
{
    Selection,      // Show selection screen
    FreshInstall,   // Demo fresh install
    Upgrade,        // Demo upgrade
    Reconfigure,    // Demo reconfigure
    Repair,         // Demo repair
    Uninstall       // Demo uninstall
}
```

---

## ?? **USAGE EXAMPLES:**

### **For Presentations/Demos:**
```powershell
# Show demo selection menu
.\EcommerceStarter.Installer.exe --demo

# Jump directly to upgrade demo
.\EcommerceStarter.Installer.exe --demo-upgrade

# Jump directly to reconfigure demo
.\EcommerceStarter.Installer.exe --demo-reconfig
```

### **For Production:**
```powershell
# Normal installation (auto-detects existing)
.\EcommerceStarter.Installer.exe

# Direct to uninstaller
.\EcommerceStarter.Installer.exe --uninstall
```

---

## ? **BENEFITS:**

1. ? **Single `--demo` flag** for all demo scenarios
2. ? **Optional specific scenarios** for quick demos
3. ? **Clear separation** between demo and production
4. ? **Safe demos** - no actual changes
5. ? **Professional presentation** mode
6. ? **Help system** built-in

---

## ?? **NEXT STEPS:**

1. Update `App.xaml.cs` with new argument parsing
2. Create `DemoSelectionWindow` 
3. Create `DemoScenario` enum
4. Update `MainWindow` to handle demo scenarios
5. Test all demo modes
6. Create batch files for quick launching:
   - `DEMO.bat` ? Launch --demo
   - `DEMO-UPGRADE.bat` ? Launch --demo-upgrade
   - etc.

---

**Want me to implement this?** ????
