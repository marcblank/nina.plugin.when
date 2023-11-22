using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin. Generate a fresh one for your plugin!
[assembly: Guid("9075c999-dacb-4c24-9e14-b696a1ac9e89")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("3.10.8.1")]
[assembly: AssemblyFileVersion("3.10.8.1")]

// [MANDATORY] The name of your plugingit st
[assembly: AssemblyTitle("Sequencer Powerups")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("Get the most out of the NINA Advanced Sequencer!")]

// The following attributes are not required for the plugin per se, but are required by the official manifest meta data

// Your name
[assembly: AssemblyCompany("Marc Blank")]
// The product name that this plugin is part ofgit 
[assembly: AssemblyProduct("When")]
[assembly: AssemblyCopyright("Copyright © 2023 Marc Blank")]

// The minimum Version of N.I.N.A. that this plugin is compatible withq
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.1056")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
// The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://bitbucket.org/zorkmid/nina.plugin.when")]

// The following attributes are optional for the official manifest meta data

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "Sequencer,Utility,Powerups,Constants,Interrupt,If,When")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://bitbucket.org/zorkmid/nina.plugin.when/downloads/Powerups.png")]
[assembly: AssemblyMetadata("ScreenshotURL", "https://bitbucket.org/zorkmid/nina.plugin.when/downloads/Constants_Screen.png")]
//[Optional] An additional url to an example example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "https://1drv.ms/u/s!AjBSqKNCEWOTgfIGHf3eIXv2hZfYAw?e=LLHMJF")]
//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"## This plugin contains a variety of potentially useful instructions that enhance the power of the Advanced Sequencer.  The set of these instructions is expected to increase over time; consider them 'utility' instructions.  Many of these instructions allow you to take arbitrary sets of actions when specific circumstances arise; you specify these actions by dragging instructions into place, just as you would to create any instruction set or template.

## Among the more powerful Powerups are those related to Constants, Variables, and Expressions, and the Template by Reference instruction.


# Complete documentation for Sequencer Powerups (in progress) is at [Powerups Docs](https://marcblank.github.io)


The following instructions are *not* yet documented in the 'complete' docs: 


Interrupt Trigger - This instruction, when dragged into a running sequence, will STOP execution after the current instruction is finished and wait for you to add any instructions you wish to execute at that time.

Autofocus Trigger - Like the above, but this will run an Autofocus instruction after the currently running instruction has finished executing (let's say you want to run an Autofocus NOW, for example)

Repeat Until All Succeed - This instruction will execute each included instruction in turn; if any fails, the sequence will wait the specified amount of time, and then start again (repeating each instruction).  This will continue until all instructions 'succeed'; consider this instruction as a series of prerequisites for the sequence to continue.

Safe Trigger - NEW for 3.7/2.7, This meta-trigger will cause the Trigger you specify to be active ONLY if a connected Safety Monitor reports 'Safe'. This instruction is intended to be used with 'When Becomes Unsafe' to prevennt unwanted Triggers from running while conditions are 'Unsafe'.

DIY Trigger - NEW for 3.7/2.7, This trigger allows you to split a specified trigger's triggering condition from the action it takes when triggered.  Yes, this is the same instruction as in the DIY Trigger plugin.


SWITCH:

If Switch/Weather - (Deprecated; use the 'If' instruction)

When Switch/Weather (Deprecated; use the 'When' instruction)

Wait until False - (Deprecated)


UTILITY:

Wait Indefinitely - Waits until you skip the instruction or delete it.  This may be useful if you need your sequence to wait for you to complete some manual task which will take an undetermined amount of time.

Breakpoint - Basically the same as Wait Indefinitely; stops sequence execution until Continue is clicked.

Comments, suggestions, bug reports, etc. are welcomed!  Contact me by DM @chatter on the NINA Discord server, or post in the #sequencer-powerups channel.
")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]