using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin. Generate a fresh one for your plugin!
[assembly: Guid("9075c999-dacb-4c24-9e14-b696a1ac9e89")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("3.1.3.0")]
[assembly: AssemblyFileVersion("3.1.3.0")]

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
[assembly: AssemblyMetadata("Tags", "Sequencer, Utility, Powerups")]

//[Optional] A link that will show a log of all changes in between your plugin's versions
[assembly: AssemblyMetadata("ChangelogURL", "https://bitbucket.org/zorkmid/nina.plugin.when/CHANGELOG.md")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://bnz07pap001files.storage.live.com/y4mYxcPQqg9HC_C5B4jDve72bZ91epqEi-LhQW2HklNx-UFzbse9svy9ulvFEApVIQgJ-iWfzgf2XcRyxLtstPMcj7sqB4dhnP4gAFPgl0TJhkYtoYrCpPeq0K_SSPehd6aPKitznJS74qAErQoJ0W2gDz6bUF_B1ujl6HbTncrtMhhXGmurFiFaLU47lbfO4E9?encodeFailures=1&width=168&height=164")]
//[Optional] A url to an example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "")]
//[Optional] An additional url to an example example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"
This plugin contains a variety of potentially useful instructions that enhance the power of the Advanced Sequencer.  The set of these instructions is expected to increase over time; consider them 'utility' instructions.  Many of these instructions allow you to take arbitrary sets of actions when specific circumstances arise; you specify these actions by dragging instructions into place, just as you would to create any instruction set or template.  (NOTE: This plugin includes all of the functionality of the 'Interrupts' plugin, which is now obsolete; it is also renamed from 'When (and If)').

These are the instructions that are current in Sequencer Powerups, broken down by the Category in which they appear in the instruction sidebar.

SEQUENCER (new Category):

Template By Reference - This powerful instruction incorporates the specified (by name) Template into your sequence/target/template at the time it is brought into the Advanced Sequencer screen.  A sequence/target/template that has one (or more) of this instruction, when saved, saves ONLY the name of the Template; in that way, you can update any of your Templates and have the updates reflected in ALL of the sequences/targets/templates that use that Template!

Interrupt Trigger - This instruction, when dragged into a running sequence, will STOP execution after the current instruction is finished and wait for you to add any instructions you wish to execute at that time.

Autofocus Trigger - Like the above, but this will run an Autofocus instruction after the currently running instruction has finished executing (let's say you want to run an Autofocus NOW, for example)

SAFETY MONITOR:

When Becomes Unsafe - This trigger is activated within SECONDS of your Safety Monitor registering an 'Unsafe' condition.  You specify the actions (instructions) to be taken in that circumstance - for example, you might want to ""Close Dome Shutter"", send yourself a message using the Ground Station plugin, and then ""Wait for Safe"".  After that, you might ""Open Dome Shutter"", send yourself another message, ""Run Autofocus"", and ""Slew and Center"".  When your instructions have finished executing, NINA will continue from where it left off!  And this instruction can be used repeatedly (the rain might start and stop and then start again...)  This instruction is likely to be much simpler to understand than multiple loops based on the 'safe' status of your gear.

If Safe - Specify instructions to execute if your Safety Monitor is currently reporting 'Safe'

If Unsafe - Specify instructions to execute if your Safety Monitor is currently reporting 'Unsafe'

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