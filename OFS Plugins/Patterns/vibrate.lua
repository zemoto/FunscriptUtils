package.path = package.path .. ";"..os.getenv('APPDATA').."\\OFS\\OFS3_data\\extensions/?.lua"
local common = require("common")

local Vibrate = {}
Vibrate.Intensity = 100
Vibrate.SpeedLimit = 400
Vibrate.MinSpeed = 40
Vibrate.MSBetweenVibrations = 30
Vibrate.ForceTop = false
Vibrate.ForceBottom = false
Vibrate.FadeOut = false
Vibrate.FadeIn = false
Vibrate.FadeRateModifier = 100
Vibrate.Error = nil

function Vibrate.gui()
	Vibrate.Intensity, intensityChanged = ofs.SliderInt("Intensity(%)", Vibrate.Intensity, 10, 100)		
	Vibrate.MSBetweenVibrations, densityChanged = ofs.SliderInt("Density(ms)", Vibrate.MSBetweenVibrations, 20, 130)	
	
	Vibrate.ForceTop, forceTopChanged = ofs.Checkbox( "Force Top", Vibrate.ForceTop )
	if Vibrate.ForceTop then
		Vibrate.ForceBottom = false
	end
	
	Vibrate.ForceBottom, forceBottomChanged = ofs.Checkbox( "Force Bottom", Vibrate.ForceBottom )
	if Vibrate.ForceBottom then
		Vibrate.ForceTop = false
	end
	
	Vibrate.FadeOut, fadeOutChanged = ofs.Checkbox( "Fade Out", Vibrate.FadeOut )
	if fadeOutChanged and Vibrate.FadeOut then
		Vibrate.FadeIn = false
	end
	
	Vibrate.FadeIn, fadeInChanged = ofs.Checkbox( "Fade In", Vibrate.FadeIn )
	if fadeInChanged and Vibrate.FadeIn then
		Vibrate.FadeOut = false
	end
	
	local fadeRateChanged = false
	if Vibrate.FadeOut or Vibrate.FadeIn then
		Vibrate.FadeRateModifier, fadeRateChanged = ofs.SliderInt("Fade Rate(%)", Vibrate.FadeRateModifier, 25, 300)
	end

	if Vibrate.Error ~= nil then
		ofs.Text("ERROR: " .. Vibrate.Error)
	end
	
	if (intensityChanged or densityChanged or forceTopChanged or forceBottomChanged or fadeOutChanged or fadeInChanged or fadeRateChanged) and ofs.Undo() then
		Vibrate.vibrate()
	end
end

function Vibrate.vibrate()
	Vibrate.Error = nil
	local script = ofs.Script(ofs.ActiveIdx())
    local targetActions = common.getTargetActions(script)
	if targetActions == nil or #targetActions == 0 then
		return
	end
	
	common.StartAddingActions()
	
	local targetTimeBetweenVibrations = Vibrate.MSBetweenVibrations / 1000
	for idx, targetAction in ipairs(targetActions) do
		local vibrationActions = Vibrate.getVibrationActions(targetAction.first, targetAction.second, targetTimeBetweenVibrations, Vibrate.Intensity, Vibrate.ForceTop, Vibrate.ForceBottom, Vibrate.FadeIn, Vibrate.FadeOut, Vibrate.FadeRateModifier)		
		if vibrationActions ~= nil then
			for k, newAction in ipairs(vibrationActions) do
				common.AddAction(targetAction.first, targetAction.second, newAction.at, newAction.pos)
			end
		end
	end
   
    if common.commitNewActions(script) then
		Vibrate.Error = nil
	end
end

function Vibrate.getVibrationActions(startAction, endAction, targetTimeBetweenVibrations, intensity, forceTop, forceBottom, fadeIn, fadeOut, fadeRateModifier)
	if (forceTop and forceBottom) or (fadeIn and fadeOut) then
		return nil
	end

	local timePoints, vibrationDistance, slope, timeBetweenVibrations = getVibrationParameters(startAction, endAction, targetTimeBetweenVibrations, intensity)
	if timePoints == nil then
		return nil
	end

	-- Ensure the vibration doesn't clip and vibrate above in those cases
	-- Also vibrate above if we are holding in the lower half (holds in the upper half stil vibrate below)
	local startPos = startAction.pos
	local endPos = endAction.pos
	local addingBottom = true
	local adjustment = -vibrationDistance	
	local holdingInLowerHalf = math.abs(startPos - endPos) <= 10 and startPos <= 50
	if forceTop or (not forceBottom and ((holdingInLowerHalf or startPos < vibrationDistance or endPos < vibrationDistance) or (endPos > startPos and endPos < 100 - vibrationDistance))) then
		adjustment = vibrationDistance
		addingBottom = false
	end
				
	startPos = startPos + adjustment
	endPos = endPos + adjustment
	
	local minVibrationDistance = math.floor(Vibrate.MinSpeed * timeBetweenVibrations + 1)
	local fadeThreshold = 2*vibrationDistance - minVibrationDistance -- Fade threshold to stay above min vibration distance
	local fadeRate = (2*vibrationDistance - minVibrationDistance) / #timePoints * (fadeRateModifier/100)
	
	local fadeAdjustment = 0 -- Rolling value indicating how much a vibration has faded
	if fadeIn then
		fadeAdjustment = fadeThreshold * (fadeRateModifier/100) -- Start at max and go down for fading in
	end
	
	local intercept = startPos - (slope * startAction.at)
	local bottomIsMainDirection = addingBottom	
	local vibrationActions = {}
	for i, time in ipairs(timePoints) do
		local centerLineAtTime = (slope * time) + intercept
		local position = 0
		if addingBottom then
			position = centerLineAtTime - vibrationDistance
			if bottomIsMainDirection then
				position = position + fadeAdjustment
			end
		else
			position = centerLineAtTime + vibrationDistance
			if not bottomIsMainDirection then
				position = position - fadeAdjustment
			end
		end
		
		if fadeOut then
			-- If the last vibration was over the threshold, we are done fading out
			-- Make sure we finish on an odd action or it will stop at an incomplete vibration
			if fadeAdjustment > fadeThreshold and i % 2 == 1 then
				break
			end
			fadeAdjustment = fadeAdjustment + fadeRate
		elseif fadeIn then
			fadeAdjustment = math.max(fadeAdjustment - fadeRate, 0)
		end				
		
		position = clamp(position, 0, 100)		
		table.insert(vibrationActions, {at=time, pos=position})		
		addingBottom = not addingBottom;			
	end
	
	return vibrationActions
end

function getVibrationParameters(startAction, endAction, targetTimeBetweenVibrations, intensity)
	local numTimePoints = math.floor(((endAction.at - startAction.at) / targetTimeBetweenVibrations) + 0.5)
	
	-- Adding an odd number of actions prevents weird end actions
	-- numTimePoints should be even since it includes the existing end action
	if numTimePoints % 2 == 1 then
		numTimePoints = numTimePoints - 1
	end
	
	::recalcTimeBetweenVibrations::
	if numTimePoints < 2 then
		Vibrate.Error = "Limitters prevented vibration"
		return nil
	end
	
	-- Don't allow vibration gaps smaller than 20ms
	local timeBetweenVibrations = (endAction.at - startAction.at) / numTimePoints;
	if timeBetweenVibrations < 0.02 then
		numTimePoints = numTimePoints - 2
		goto recalcTimeBetweenVibrations
	end
	
	local slope = (endAction.pos - startAction.pos) / (endAction.at - startAction.at)
	local vibrationDistance = math.max(math.floor((Vibrate.SpeedLimit * (intensity / 100.0) * timeBetweenVibrations / 2) + 0.5), 3)
	
	-- Ensure the speed of the large action is below the speed limit
	local vibrationSpeed = nil
	repeat
		local largeActionDistance = getLargeActionDistance(vibrationDistance, timeBetweenVibrations, slope)			
		vibrationSpeed = getSpeedBetweenActions({at=0, pos=0},{at=timeBetweenVibrations, pos=largeActionDistance})
		if vibrationSpeed > Vibrate.SpeedLimit then
			vibrationDistance = vibrationDistance - 1
		end
		if vibrationDistance < 3 then
			numTimePoints = numTimePoints - 2
			goto recalcTimeBetweenVibrations
		end
	until(vibrationSpeed <= Vibrate.SpeedLimit)
	
	-- Ensure the small actions are above the min speed limit
	local smallActionDistance = getSmallActionDistance(vibrationDistance, timeBetweenVibrations, slope)
	if getSpeedBetweenActions({at=0, pos=0},{at=timeBetweenVibrations, pos=smallActionDistance}) < Vibrate.MinSpeed then
		numTimePoints = numTimePoints - 2
		goto recalcTimeBetweenVibrations
	end
			
	local timePoints = {}
	local currentTime = startAction.at + timeBetweenVibrations;
	for j=1,numTimePoints-1 do
		table.insert(timePoints, currentTime)
		currentTime = currentTime + timeBetweenVibrations;
	end
	
	return timePoints, vibrationDistance, slope, timeBetweenVibrations
end

function getLargeActionDistance(vibrationDistance, timeBetweenVibrations, slope)
	local largeActionDistance = nil
	if slope >= 0 then			
		largeActionDistance = (slope * timeBetweenVibrations) + vibrationDistance*2
	else
		largeActionDistance = (slope * timeBetweenVibrations) - vibrationDistance*2
	end
	
	return math.ceil(math.abs(largeActionDistance))
end

function getSmallActionDistance(vibrationDistance, timeBetweenVibrations, slope)
	local smallActionDistance = nil
	if slope >= 0 then			
		smallActionDistance = (slope * timeBetweenVibrations) - vibrationDistance*2
	else
		smallActionDistance = (slope * timeBetweenVibrations) + vibrationDistance*2
	end
	
	return math.ceil(math.abs(smallActionDistance))
end

function getSpeedBetweenActions(first, second)
	local distance = second.at - first.at;
	local change = math.abs(second.pos - first.pos)
	return change / distance
end

return Vibrate