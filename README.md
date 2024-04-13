# Sequencer Powerups Version History

Sequencer Powerups 3.13.7.x

* New implementation of discovery of available data Variables (hovering over the "i" icon wherever an Expression is allowed).  Comments welcomed.
* Fixed an issue in which, at the end of When Becomes Unsafe, a main sequence instruction would start running for a few seconds in the case that an Unsafe condition was again present.
Image

3.9.3.0

 - Fixed Take Exposure + (along with Take Many Exposures and Smart Expoure) so that they play nicely with exposure counts
 
 - (Work in Progress) Added FWHM, HFR, StarCount, and Eccentricity to values that can be used in Expressions.  This data refers to the most recently analyzed image.  Since this can take a number of seconds after an exposure is complete, you should consider putting some sort of Wait instruction between a Take Exposure instruction and an If instruction using values from that exposure.  Also, these values only work with Take Exposure + (not other exposure instructions); this is only a temporary situation.  Also note that Take Many Exposures and Smart Exposure will complete ALL of the exposures and only the most recent exposure will be used for values in expressions.  So please consider these things before using this new feature!

