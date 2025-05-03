local matchPatternModule = require("matchPattern")
local matchPatternEXModule = require("matchPatternEX")
local hardModeModule = require("hardMode")
local bookendModule = require("bookend")
local centerModule = require("center")
local groundModule = require("ground")
local raiseModule = require("raise")

local HeroPatterns = {}
HeroPatterns.SelectedPatternIdx = 1

local MatchPattern = {}
MatchPattern.IsHardMode = false

local FixTiming = {}
FixTiming.BPM = 128

local Center = {}
Center.Max = 100
Center.Min = 0

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Apply Pattern" ) then
		applyPattern()
	end
	
	ofs.Separator()

	HeroPatterns.SelectedPatternIdx, changed = ofs.Combo( "", HeroPatterns.SelectedPatternIdx, { "Match Pattern", "Match Pattern EX", "Hard Mode", "Bookend", "Center", "Ground", "Raise" } )
	
	if HeroPatterns.SelectedPatternIdx == 1 then -- Match Pattern
		MatchPattern.IsHardMode, isHardModeChanged = ofs.Checkbox( "Script is hard mode", MatchPattern.IsHardMode )
	elseif HeroPatterns.SelectedPatternIdx == 2 then -- Match Pattern EX
		matchPatternEXModule.HeroMax, changes = ofs.SliderInt("Max", matchPatternEXModule.HeroMax, 10, 100)
	elseif HeroPatterns.SelectedPatternIdx == 5 then -- Center
		Center.Max, maxChanged = ofs.SliderInt("Max", Center.Max, 10, 100)
		Center.Min, minChanged = ofs.SliderInt("Min", Center.Min, 0, 90)
	end
end

function applyPattern()
	if HeroPatterns.SelectedPatternIdx == 1 then -- Match Pattern
		if MatchPattern.IsHardMode then
			matchPatternModule.matchPattern.hard()
		else
			matchPatternModule.matchPattern.easy()
		end
	elseif HeroPatterns.SelectedPatternIdx == 2 then -- Match Pattern EX
		matchPatternEXModule.apply()
	elseif HeroPatterns.SelectedPatternIdx == 3 then -- Hard Mode
		hardModeModule.hardMode()
	elseif HeroPatterns.SelectedPatternIdx == 4 then -- Bookend
		bookendModule.bookend()
	elseif HeroPatterns.SelectedPatternIdx == 5 then -- Center
		centerModule.center(Center.Min,Center.Max)
	elseif HeroPatterns.SelectedPatternIdx == 6 then -- Ground
		groundModule.ground()
	elseif HeroPatterns.SelectedPatternIdx == 7 then -- Raise
		raiseModule.raise()
	end
end