import React, { useState, useEffect, useRef } from 'react';
import { Calendar, MapPin, Clock, Car, Check, Sparkles } from 'lucide-react';

/**
 * StreamingTripContent - Displays trip data with Gemini-like typing effect
 * @param {Object} tripData - The complete trip plan data
 * @param {Function} onComplete - Called when all animations finish
 */
const StreamingTripContent = ({ tripData, onComplete }) => {
    const [displayedTitle, setDisplayedTitle] = useState('');
    const [displayedDescription, setDisplayedDescription] = useState('');
    const [displayedDays, setDisplayedDays] = useState([]);
    const [currentPhase, setCurrentPhase] = useState('title'); // title -> description -> days
    const [currentDayIndex, setCurrentDayIndex] = useState(0);
    const [currentLocationIndex, setCurrentLocationIndex] = useState(0);
    const [isComplete, setIsComplete] = useState(false);
    
    const TYPING_SPEED = 15; // ms per character
    const WORD_SPEED = 50; // ms per word for faster sections
    
    // Typewriter effect for title
    useEffect(() => {
        if (currentPhase !== 'title' || !tripData?.title) return;
        
        let i = 0;
        const interval = setInterval(() => {
            setDisplayedTitle(tripData.title.slice(0, i + 1));
            i++;
            if (i >= tripData.title.length) {
                clearInterval(interval);
                setTimeout(() => setCurrentPhase('description'), 300);
            }
        }, TYPING_SPEED);
        
        return () => clearInterval(interval);
    }, [currentPhase, tripData?.title]);
    
    // Typewriter effect for description (word by word for speed)
    useEffect(() => {
        if (currentPhase !== 'description' || !tripData?.description) return;
        
        const words = tripData.description.split(' ');
        let i = 0;
        const interval = setInterval(() => {
            setDisplayedDescription(words.slice(0, i + 1).join(' '));
            i++;
            if (i >= words.length) {
                clearInterval(interval);
                setTimeout(() => setCurrentPhase('days'), 500);
            }
        }, WORD_SPEED);
        
        return () => clearInterval(interval);
    }, [currentPhase, tripData?.description]);
    
    // Reveal days one by one
    useEffect(() => {
        if (currentPhase !== 'days' || !tripData?.days) return;
        
        if (currentDayIndex < tripData.days.length) {
            const timer = setTimeout(() => {
                setDisplayedDays(prev => [...prev, { 
                    ...tripData.days[currentDayIndex], 
                    locations: [] 
                }]);
                setCurrentLocationIndex(0);
                setCurrentPhase('locations');
            }, 300);
            return () => clearTimeout(timer);
        } else {
            // All days done
            setIsComplete(true);
            if (onComplete) onComplete();
        }
    }, [currentPhase, currentDayIndex, tripData?.days]);
    
    // Reveal locations one by one within current day
    useEffect(() => {
        if (currentPhase !== 'locations' || !tripData?.days) return;
        
        const currentDay = tripData.days[currentDayIndex];
        if (!currentDay) return;
        
        if (currentLocationIndex < currentDay.locations.length) {
            const timer = setTimeout(() => {
                setDisplayedDays(prev => {
                    const updated = [...prev];
                    if (updated[currentDayIndex]) {
                        updated[currentDayIndex] = {
                            ...updated[currentDayIndex],
                            locations: [
                                ...updated[currentDayIndex].locations,
                                currentDay.locations[currentLocationIndex]
                            ]
                        };
                    }
                    return updated;
                });
                setCurrentLocationIndex(prev => prev + 1);
            }, 200);
            return () => clearTimeout(timer);
        } else {
            // All locations in current day done, move to next day
            setCurrentDayIndex(prev => prev + 1);
            setCurrentPhase('days');
        }
    }, [currentPhase, currentDayIndex, currentLocationIndex, tripData?.days]);

    if (!tripData) {
        return (
            <div className="flex items-center justify-center p-8">
                <div className="animate-pulse flex items-center gap-2 text-blue-600">
                    <Sparkles className="animate-spin" size={20} />
                    <span>Đang tải dữ liệu...</span>
                </div>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto p-6 space-y-8">
            {/* Header */}
            <div className="bg-linear-to-r from-blue-600 to-cyan-500 rounded-2xl p-8 text-white shadow-xl">
                <h1 className="text-3xl font-bold mb-4 min-h-10">
                    {displayedTitle}
                    {currentPhase === 'title' && (
                        <span className="animate-pulse ml-1">|</span>
                    )}
                </h1>
                
                {displayedDescription && (
                    <p className="text-white/90 leading-relaxed">
                        {displayedDescription}
                        {currentPhase === 'description' && (
                            <span className="animate-pulse ml-1">|</span>
                        )}
                    </p>
                )}
                
                {/* Meta info */}
                {currentPhase !== 'title' && (
                    <div className="flex items-center gap-6 mt-6 text-sm text-white/80 animate-in fade-in duration-500">
                        <span className="flex items-center gap-2">
                            <Calendar size={16} />
                            {tripData.start} - {tripData.end}
                        </span>
                        <span className="flex items-center gap-2">
                            <MapPin size={16} />
                            {tripData.days?.length} Ngày
                        </span>
                    </div>
                )}
            </div>

            {/* Days */}
            <div className="space-y-6">
                {displayedDays.map((day, dayIdx) => (
                    <div 
                        key={dayIdx}
                        className="bg-white rounded-xl shadow-lg border border-slate-100 overflow-hidden animate-in slide-in-from-bottom-4 duration-500"
                    >
                        {/* Day Header */}
                        <div className="bg-slate-50 px-6 py-4 border-b border-slate-100">
                            <h2 className="text-lg font-bold text-slate-800">
                                {day.title}
                            </h2>
                            <p className="text-sm text-slate-500 flex items-center gap-2 mt-1">
                                <Calendar size={14} />
                                {day.date}
                            </p>
                        </div>
                        
                        {/* Locations */}
                        <div className="divide-y divide-slate-50">
                            {day.locations.map((location, locIdx) => (
                                <div 
                                    key={locIdx}
                                    className="p-4 hover:bg-slate-50 transition-colors animate-in fade-in slide-in-from-left-2 duration-300"
                                >
                                    <div className="flex items-start gap-4">
                                        {/* Time Column */}
                                        <div className="flex flex-col items-center min-w-[60px]">
                                            <div className="text-xs font-medium text-blue-600 bg-blue-50 px-2 py-1 rounded">
                                                {location.startTime?.slice(0, 5)}
                                            </div>
                                            <div className="w-px h-4 bg-slate-200 my-1"></div>
                                            <div className="text-xs text-slate-400">
                                                {location.endTime?.slice(0, 5)}
                                            </div>
                                        </div>
                                        
                                        {/* Content */}
                                        <div className="flex-1">
                                            <h3 className="font-semibold text-slate-800 mb-1">
                                                {location.name || `Địa điểm ${locIdx + 1}`}
                                            </h3>
                                            <p className="text-sm text-slate-600 leading-relaxed">
                                                {location.description}
                                            </p>
                                            
                                            {/* Transport Mode */}
                                            {location.transportMode && (
                                                <div className="flex items-center gap-1 mt-2 text-xs text-slate-400">
                                                    <Car size={12} />
                                                    <span className="capitalize">{location.transportMode}</span>
                                                </div>
                                            )}
                                        </div>
                                        
                                        {/* Order Badge */}
                                        <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center text-sm font-bold shrink-0">
                                            {location.orderIndex || locIdx + 1}
                                        </div>
                                    </div>
                                </div>
                            ))}
                            
                            {/* Loading indicator for more locations */}
                            {currentPhase === 'locations' && currentDayIndex === dayIdx && (
                                <div className="p-4 flex items-center gap-2 text-blue-500 text-sm animate-pulse">
                                    <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce"></div>
                                    <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce" style={{ animationDelay: '0.1s' }}></div>
                                    <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }}></div>
                                </div>
                            )}
                        </div>
                    </div>
                ))}
                
                {/* Loading next day indicator */}
                {currentPhase === 'days' && currentDayIndex < (tripData.days?.length || 0) && (
                    <div className="bg-white rounded-xl shadow-lg border border-slate-100 p-6 animate-pulse">
                        <div className="flex items-center gap-3 text-slate-400">
                            <Sparkles className="animate-spin" size={20} />
                            <span>Đang tải ngày tiếp theo...</span>
                        </div>
                    </div>
                )}
            </div>

            {/* Completion indicator */}
            {isComplete && (
                <div className="text-center py-6 animate-in fade-in zoom-in duration-500">
                    <div className="inline-flex items-center gap-2 px-6 py-3 bg-green-100 text-green-700 rounded-full font-medium">
                        <Check size={20} />
                        Lịch trình đã hoàn tất!
                    </div>
                </div>
            )}
        </div>
    );
};

export default StreamingTripContent;
