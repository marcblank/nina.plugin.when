using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin. Generate a fresh one for your plugin!
[assembly: Guid("9075c999-dacb-4c24-9e14-b696a1ac9e89")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("3.5.1.0")]
[assembly: AssemblyFileVersion("3.5.1.0")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Sequencer Powerups")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("Useful instructions to get the most out of the NINA Advanced Sequencer")]

// The following attributes are not required for the plugin per se, but are required by the official manifest meta data

// Your name
[assembly: AssemblyCompany("Marc Blank")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("When")]
[assembly: AssemblyCopyright("Copyright © 2023 Marc Blank")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.1000")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
// The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://bitbucket.org/zorkmid/nina.plugin.when")]

// The following attributes are optional for the official manifest meta data

//[Optional] Your plugin homepage URL - omit if not applicaple
[assembly: AssemblyMetadata("Homepage", "https://mypluginwebsite.com/")]

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "Sequencer,Utility,Powerups,Constants,Interrupt,If,When")]

//[Optional] A link that will show a log of all changes in between your plugin's versions
[assembly: AssemblyMetadata("ChangelogURL", "https://bitbucket.org/zorkmid/nina.plugin.when/CHANGELOG.md")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://bitbucket.org/zorkmid/nina.plugin.when/downloads/Powerups.png")]
[assembly: AssemblyMetadata("ScreenshotURL", "https://bitbucket.org/zorkmid/nina.plugin.when/downloads/Constants_Screen.png")]
//[Optional] An additional url to an example example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "https://1drv.ms/u/s!AjBSqKNCEWOTgfIGHf3eIXv2hZfYAw?e=LLHMJF")]
//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"This plugin contains a variety of potentially useful instructions that enhance the power of the Advanced Sequencer.  The set of these instructions is expected to increase over time; consider them 'utility' instructions.  Many of these instructions allow you to take arbitrary sets of actions when specific circumstances arise; you specify these actions by dragging instructions into place, just as you would to create any instruction set or template.  (NOTE: This plugin includes all of the functionality of the 'Interrupts' plugin, which is now obsolete; it is also renamed from 'When (and If)').

These are the instructions that are current in Sequencer Powerups, broken down by the Category in which they appear in the instruction sidebar.

CONSTANTS ENHANCED (new Category):

** NOTE: Sequencer Powerups now supercedes the 'Constants for Exposures' and 'Interrupts' plugins.  Instructions from the deprecated plugins will no longer work if you remove that plugin; you will have to replace them with instructions from here! **

Define Constant - Define a Constant that can be used in the other 'Constants Enhanced' instructions.  The value of constants can include references to other, previously defined, constants.  In the plugin options page, there is the option to add up to four 'global' constants; constants that will be available in all sequences.  Values for constants can be arbitrarily complex arithmetic expressions, including the use of parentheses, not that I see any value in this...

If Constant - Executes the specified instructions if the provided Constant is 'true'

Cool Camera - Same as built-in instruction, with the ability to use a constant for temperature

Take Exposure +/Take Many Exposures +/Smart Exposure + -  These three instructions are effectively copies of the built-in instructions, with the ability to use constants for iterations (smart/many), exposure time, gain, and dither.

Screenshot: [Constants Example](https://1drv.ms/u/s!AjBSqKNCEWOTgfIEvcjHtr65Jl7W9Q?e=FM4WRF)

FOCUSER:

Move Focuser Relative + has the same functionality as NINA's Move Focuser Relative, but with the ability to use Constants

SAFETY MONITOR:

When Becomes Unsafe - This trigger is activated within SECONDS of your Safety Monitor registering an 'Unsafe' condition.  You specify the actions (instructions) to be taken in that circumstance - for example, you might want to ""Close Dome Shutter"", send yourself a message using the Ground Station plugin, and then ""Wait for Safe"".  After that, you might ""Open Dome Shutter"", send yourself another message, ""Run Autofocus"", and ""Slew and Center"".  When your instructions have finished executing, NINA will continue from where it left off!  And this instruction can be used repeatedly (the rain might start and stop and then start again...)  This instruction is likely to be much simpler to understand than multiple loops based on the 'safe' status of your gear.

If Safe - Specify instructions to execute if your Safety Monitor is currently reporting 'Safe'

If Unsafe - Specify instructions to execute if your Safety Monitor is currently reporting 'Unsafe'

Screnshot: [When Becomes Unsafe Example](https://1drv.ms/u/s!AjBSqKNCEWOTgfIGHf3eIXv2hZfYAw?e=LLHMJF)

SEQUENCER (new Category):

Template By Reference - This powerful instruction incorporates the specified (by name) Template into your sequence/target/template at the time it is brought into the Advanced Sequencer screen.  A sequence/target/template that has one (or more) of this instruction, when saved, saves ONLY the name of the Template; in that way, you can update any of your Templates and have the updates reflected in ALL of the sequences/targets/templates that use that Template!

Interrupt Trigger - This instruction, when dragged into a running sequence, will STOP execution after the current instruction is finished and wait for you to add any instructions you wish to execute at that time.

Autofocus Trigger - Like the above, but this will run an Autofocus instruction after the currently running instruction has finished executing (let's say you want to run an Autofocus NOW, for example)

Repeat Until All Succeed - This instruction will execute each included instruction in turn; if any fails, the sequence will wait the specified amount of time, and then start again (repeating each instruction).  This will continue until all instructions 'succeed'; consider this instruction as a series of prerequisites for the sequence to continue.

SWITCH:

If Switch/Weather - This instruction executes the actions you specify when an expression evaluates to true.  The expression can contain the names of Switches, Gauges, and Weather information; the accepted names are shown if you hover over the 'i' icon.  Note that any kind of arithmetic or logical operators can be used in the expression.  Hover over the expression to see its current value.

When Switch/Weather - This is the trigger equivalent of 'If Switch/Weather'; it will trigger within 5 seconds of your expression becoming true (see above).  The 'Once Only' switch indicates whether you want this instruction to be limited to a single use (remember that the instruction might be triggered repeatedly depending on what steps have been taken to make the expression 'false').

Wait until False - When used within a 'When Switch/Weather' or 'If Switch/Weather' instruction, will wait until the triggering condition becomes false.

Screenshot: [If Switch/Weather Example](https://1drv.ms/u/s!AjBSqKNCEWOTgfN26vDK79WD1gUWBw?e=31xqYu)

UTILITY:

Wait Indefinitely - Waits until you skip the instruction or delete it.  This may be useful if you need your sequence to wait for you to complete some manual task which will take an undetermined amount of time.


Comments, suggestions, bug reports, etc. are welcomed!  Contact me by DM @chatter on the NINA Discord server, or post in the #plugin-discussions channel.
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