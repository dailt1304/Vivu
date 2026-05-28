import React, { useState, useRef, useEffect } from "react";

const OtpInput = ({ length = 6, onComplete }) => {
  const [otp, setOtp] = useState(new Array(length).fill(""));
  const inputRefs = useRef([]);

  useEffect(() => {
    if (inputRefs.current[0]) {
      inputRefs.current[0].focus();
    }
  }, []);

  const handleChange = (index, e) => {
    const value = e.target.value;
    if (isNaN(value)) return;

    const newOtp = [...otp];
    // Allow only one digit
    newOtp[index] = value.substring(value.length - 1);
    setOtp(newOtp);

    // Call onComplete
    const combinedOtp = newOtp.join("");
    if (combinedOtp.length === length) {
      onComplete(combinedOtp);
    }

    // Move to next input if current field is filled
    if (value && index < length - 1 && inputRefs.current[index + 1]) {
      inputRefs.current[index + 1].focus();
    }
  };

  const handleClick = (index) => {
    inputRefs.current[index].setSelectionRange(1, 1);
    // Optional: if previous empty, move focus there
    if (index > 0 && !otp[index - 1]) {
      inputRefs.current[otp.indexOf("")]?.focus();
    }
  };

  const handleKeyDown = (index, e) => {
    if (
      e.key === "Backspace" &&
      !otp[index] &&
      index > 0 &&
      inputRefs.current[index - 1]
    ) {
      // Move to previous input on backspace
      inputRefs.current[index - 1].focus();
    }
  };

  return (
    <div className="flex gap-2 justify-center my-4">
      {otp.map((value, index) => (
        <input
          key={index}
          type="text"
          ref={(input) => (inputRefs.current[index] = input)}
          value={value}
          onChange={(e) => handleChange(index, e)}
          onClick={() => handleClick(index)}
          onKeyDown={(e) => handleKeyDown(index, e)}
          className="w-12 h-12 text-center text-xl font-bold bg-gray-50 dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:border-primary focus:ring-1 focus:ring-primary outline-none transition-all"
        />
      ))}
    </div>
  );
};

export default OtpInput;
