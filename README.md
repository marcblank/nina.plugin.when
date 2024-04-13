# Sequencer Powerups Version History

All .x versions include minor bug fixes

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

