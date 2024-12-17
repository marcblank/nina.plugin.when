# Sequencer Powerups Version History

All .x versions include minor bug fixes



3.20.0.x

*New instruction Smart Subframe Exposure + adds the ability to use a subframe - either an ROI percentage or specific dimensions (X, Y, Width, Height) - with all of the other features of Smart Exposure +

*String Variables are now available in Powerups.  These work with the Variable and Set Variable instructions at present. String values are written with single quotes, e.g. 'This is a legal string value'

*String Variables can only be used with the Send via Ground Station and External Script + instructions

*Added TargetName Variable, which can be used when a target is currently active

*Fixed a bug with Trained Dark Flat Exposure + where the wrong filter might get used

*Fixed a bug that could, in very unusual circumstances, cause When Becomes Unsafe to be triggered inappropriately.

*Started work on communication with Target Scheduler to introduce data from that plugin (current TargetName variable is taken from the sequencer directly, not TS)

3.19.x.x

*New instruction Smart Subframe Exposure + adds the ability to use a subframe - either an ROI percentage or specific dimensions (X, Y, Width, Height) - with all of the other features of Smart Exposure +

*Fixed a bug with Trained Dark Flat Exposure +

*String Variables are now available in Powerups.

*These work with the Variable and Set Variable instructions at present; Constant and Global Variable do not accept strings.

*Strings are written with single quotes, e.g. 'This is a legal string value'

*String Variables can only be used with the Send via Ground Station and External Script + instructions

3.18.0.x

*You can now use Send via Ground Station with the MQTT Broker and UDP

*Formatting changes for consistency in the  "info" popup

*Fixed issue with Trained Flat Exposure + and Variables

3.16.1.x

*Added For Each instruction; it's documented in the Powerups docs at https://marcblank.github.io/   The instruction appears to work, but syntax is important - you really should look at the docs...

3.16.0.x

*Significant changes to Powerups UI related to errors and warnings in Expressions.  Instead of showing the actual errors/warnings inline in sequences, they are now indicated by warning icons (orange for warning and red for error) that can be hovered over to see the actual error.  In addition, errors are now always propagated to NINA's error icon so that attempting to run a sequence with an Expression error will cause the standard NINA warning to come up.

*Added Side of Pier to data Variables (along with PierEast, PierWest, and PierUnknown)

3.14.16.x

*Bug fixes for Global Variables

*Added MoonIllumination to data variables (values are 0 to 1)

3.14.15.x

*Reverts recent versions that had issues with Variables being renamed with suffixes (like _0).   Changed variables will need to be modified by hand

3.14.8.x

*Significant revamp of DIY Meridian Trigger; please report any issues you find and I'll get on them ASAP.   This should work better with multiple targets, Target Scheduler, and when used globally.

*There are some new "hidden" Variables:  camera__XSize, camera_YSize, and camera_PixelSize (hidden in that they don't appear in the "info" popup)

3.14.7.x

*Fixed a regression related to Smart Exposure + for folks with OSC cameras

*More features in the Powerups Panel; better UI for deleting items and the ability to determine, for each, whether the value for an Expression should be displayed as a number, boolean, or filter name

*Fixed bug in which an instruction requiring rotation (like Slew, Center, and Rotate) would lose the rotation if within a Once Safe instruction

*Fixed an issue with Image_HFR in the Powerups Panel

*Fixed bug with retrieving Image_FWHM and Image_Eccentricity in Expressions

3.14.6.x

*Fixed bug related to the new Send via Ground Station and using the $$TARGET_NAME$$ token

*Preliminary version of a new imaging panel "dockable" - Powerups Panel in which you can add any number of Expressions that will be updated in real time - these can include any of the various data Variables exposed by Powerups as well as Constants and user-defined Variables to the extent they are valid in a running sequence.   This might be a good way to unclutter your imaging panel in some cases.

*The UI for Powerups Panel is pretty primitive at the moment.  A work in progress.

3.14.5.x

*Image Variables now work with "core" exposure instructions: Image_HFR, Image_StarCount, and Image_Eccentricity / Image_FWHM if using Hocus Focus

*Newly added Image_Id is a unique (per session) identifier for the image to which these Variables refer.  This id is incremented with each exposure, but has no other meaning; it can be used, for example, to see if there is a new image whose data is available to Powerups.

*Bug fixes related to Trained Flat Exposure + and Trained Dark Flat Exposure +

*Cleaned up some logging

3.14.4.x

*When Becomes Unsafe and When triggers will now find and use the current target, i.e. the target active at the time the trigger starts running, when those triggers are placed globally or in an instruction set "above" the currently running target.   This means that it's no longer necessary to save/restore mount position when using Slew and Center or Slew, Center, and Rotate within these triggers.

3.14.2.x

*New instruction: Send via Ground Station allows you to include Expressions in messages that will be sent via Ground Station (Send Email, Send to Pushover, Send to Telegram, and Send to TTS)  This instruction is somewhat experimental and will eventually be replaced with an implementation using the new Pub/Sub feature of NINA.   To add an Expression to your message, surround it with curly brackets, as in the example below.   This is most useful for sending information about Variables, since most other data is already handled in Ground Station.  All regular Ground Station tokens work as usual.

*Wait for Time Span + now works with zero-length waits.

3.14.0.x

*Added preliminary support for dates/times in Variables and Expressions.

*New instruction jSet Variable to Time which uses the same UI as "Wait for Time" (so you can specify times relative to various Sunset/Sunrise/Meridian times)

*New functions (for use in Expressions): now(), hour(), minute(), day(), month(), year(), and dow().   These functions (excepting now) can take a  date/time, so year(now()) would return 2024.

3.13.9.x 

*Added Add Image Pattern instruction.   You provide a name for the pattern (e.g. FOO) and a new image pattern $$FOO$$ becomes available for use when you take images.  The Expression you provide in the instruction is evaluated when an image file is created, and that value is used as the value of the pattern you added.

*Note: The pattern is added once a name, value (any Expression you like), and a description are added to the instruction.

*Note: The instruction is self-executing, like Constant.  It does not need to be executed; just adding or loading it is enough.

*Note:  There's currently no way to change or remove a file pattern token, so if you change the name or description inside the Add Image Pattern instruction, nothing will happen (at least until you start NINA again)

*Note: This restriction will be removed at some point in the future when NINA supports its

3.13.8.x

*New : Conditional Trigger enables the embedded Trigger when an Expression is true (so you can turn your Triggers on and off)

*Added Variables MoonAltitude, SunAltitude, and AtPark

*Added Variables RightAscension and Declination

3.13.7.x

* New implementation of discovery of available data Variables (hovering over the "i" icon wherever an Expression is allowed).  Comments welcomed.

* Fixed an issue in which, at the end of When Becomes Unsafe, a main sequence instruction would start running for a few seconds in the case that an Unsafe condition was again present.

3.13.6.x

* Added ROI (sub-exposure %) to the Smart Exposure + instruction, defaulting (of course) to 100.  Note: not all cameras are capable of handing sub-exposures.

3.13.5.x

* Two new instructions:  Slew To Alt/Az + and Slew to RA/Dec +.  Both of these instructions work with decimal degrees, unlike the standard NINA instructions.  Altitude and Azimuth are available as data symbols that can be saved in Variables

* Bug fixed related to moving Constant/Variables definitions (.1)

* Added DomeAzimuth as a data item (.4)

* Allow decimal rotation values (.4) in Rotate by Mechanical Angle
 
3.13.4.x

* Added Rotate by Mechanical Angle + instruction

* Added Slew to RA/Dec which will shortly be renamed to avoid confusion with Slew to Ra/Dec (sigh).  The new instruction uses decimal degrees and works with Expressions.   Sorry for the confusion.

3.13.3.x

* New instruction Annotation + allows Expressions to be used within the text; surround them with {}, e.g. {FocuserPosition}
 
3.13.1.x

* New feature for the When trigger - an "Interrupt" toggle (default 'ON' for compatibility); turning this to OFF will cause When to act like other triggers, and only act between execution of other instructions (rather than interrupting them)

* Fix edge case issue with restoring Templates with Switch Filter + command

3.13.0.x

* Fixed rare bug with loading DIYMF in a sequence

3.13.0.x

* New Call and Return instructions implementing Functions

* Documentation is  here: https://marcblank.github.io/Functions/

* Feel free to play; I have no idea what it's good for! 😂

3.12.4.x

* New instruction, If Timeout, wraps an instruction, instruction set, or Template and times out in a specified number of seconds.  If a timeout occurs, the instructions you add will be executed.   As new functionality, please report any untoward behavior.   Comments, etc. very much appreciated!

3.12.2.x

* New instruction External Script + which is a potentially powerful new tool for users of external scripts; this is fully documented at  https://marcblank.github.io/External%20Script%20%2B/   Thanks to @Hologram for the suggestion and for the documentation!

* New instruction Wait for Time Span + does what you'd expect.

* Some users of ROR observatories (especially shared ones like SRO) might benefit from a new feature added to 3.12.1.0 - the ability to read a file containing roof status.   In the plugin page, choose a file and a string to look for when the roof is open.   Powerups will read the last (only?) line in the file and look for the (case-insensitive) string you specify.   Powerups will define a RoofStatus variable with values RoofOpen, RoofNotOpen, and RoofCannotOpenOrRead and make that available in all Expressions.   This feature is new and somewhat experimental - let me know how it works, any desired enhancements, etc. 

3.12.0.x

* A completely new implementation of Constants, Variables, and Expressions.

* Define Constant and Define Variable have been renamed Constant and Variable

* There is an IsSafe variable that tracks the Safety Monitor; the 'If" instruction can be used with this as a replacement for If Safe and If Unsafe (which are deprecated)

* New variables Image_HFR, Image_StarCount (and Image_FWHM and Image_Eccentricity for Hocus Focus users) are available; they refer to the most recent image taken with Take Exposure +, Take Many Exposures +, and Smart Exposure +.   For now, the 'If' command can be used with these new variables; the 'If' command will wait for processing of the most recent image to complete!

* Bug fixes related to log spam and Smart Exposure +/Filters (picking the wrong filter was possible)

* DIY Meridian Flip no longer needs to be inside a DSO container and can even be a global trigger

3.11.0.x

* Requires NINA 3.0 Beta

* Fixes (with any luck) the WBU issue in which the play/stop button doesn't work properly (kudos to Stefan!)

3.10.16.x

* New instruction: Log (allows you to annotate the NINA log; can be helpful for debugging)

* New instruction: Flip Rotator (some people need to do this after a Meridian Flip (DIYMF or regular))

* Now accepting coffee, lol...

3.10.15.x

* New feature: Filters can now be used with Constants and Variables in the various + instructions

* New instruction: Switch Filter +

* New instruction: End Instruction Set that can terminate any instruction set in the hierarchy of running instructions.

3.10.13.x

* DIY Meridian Flip is now part of Powerups with a more compact look.  The old DIYMF plugin will continue to exist, though future enhancements will only be made to Powerups

3.10.12.x

* Added flat panel cover states, which can be used in If statements, for example

3.10.11.x

* Powerups instructions appear correctly in the mini-sequencer (in the Imaging page)

* Powerups instructions play nicely with plugins like Orbuculum that "walk" through sequences

3.10.7.x

* Global Constants are now optionally per profile; the default is that all profiles are affected.

3.10.0.x

* Added an IfThenElse instruction that does what you'd expect

* Deprecated "Wait until False"

* Moved Powerups instructions into new categories (just to mix things up).  All Powerups instructions are in categories starting with "Powerups".

3.9.6.x

* The "When Switch" trigger is now just "When", it's a trigger that trips within seconds of an arbitrary Expression becoming true
Compare "When" to "If", which is an instruction that is executed in turn within a sequence

* The "Duration" value in Cool Camera + can now be an Expression

* A new Variable TIME is the time in seconds since NINA was started.  It can be used in Expressions to, for example, create timers.. 😱

* ShutterStatus is now available in Expressions (along with gauge, switch, weather, and SensorTemp); you'll need to use the integer values for now

3.9.5.x

* Added support for a SAFE variable as a poor man's safety monitor wherein you can signal safe/unsafe using other data that's available (weather data, switch data, image data, etc.)  Details in the documentation at https://marcblank.github.io/Safety/#the-safe-variable

3.9.3.x

* Early implementation of image data that can be used in Expressions.  See https://bitbucket.org/zorkmid/nina.plugin.when/src/master/README.md for details

3.9.1.x

* Gauge, Switch, and Weather data can now be used in Expressions (Constants, Variables, If and Loop While instructions)

3.9.0.x

* Please report issues, comments, suggestions to the shiny new ⁠sequencer-powerups channel.

* New instructions:  Define Variable, Set Variable, Loop Until  <expression> (a Condition), Breakpoint

* If Constant renamed to simply If

* Document explaining Constants, Variables, and Expressions (see previous post); worth reading even if you've used Constants before!

* Template By Reference and If (formerly If Constant) take up FAR less room horizontally (thanks, Stefan!)

3.8.1.x

* New instruction "Once Safe", to be used with "When Becomes Unsafe".   This instruction, when run inside a "When Becomes Unsafe", runs the enclosed set of instructions once conditions again become 'safe'.  If an 'unsafe' condition occurs within these instructions, the instructions will stop immediately and control will be returned to the "When Becomes Unsafe" set of instructions.

3.7.0.x

* Incorporated DIYTrigger from the eponymous plugin (with its author's blessing).  A very handy way to split the triggering condition from its actions.

* Newly added SafeTrigger instruction will only run the specified trigger if the Safety Monitor (if any) reports a 'Safe' condition.   This is intended to be used with the 'When Becomes Unsafe' trigger to ensure that a potentially long-running trigger isn't run during the 'When Becomes Unsafe' set of instructions
