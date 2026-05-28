import React from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useLocationCategories } from "../../../hooks/locations/useLocations";

import StepIndicator from "./StepIndicator";
import DestinationStep from "./steps/DestinationStep";
import PreferencesStep from "./steps/PreferencesStep";
import DetailsStep from "./steps/DetailsStep";

const CreateTripWizard = ({ formHook, tripLimitInfo }) => {
  const { step, data, errors, setFormData, nextStep, prevStep, submitForm } =
    formHook;

  // Pre-fetch categories as early as possible so there's no loading delay on Step 2
  useLocationCategories();

  return (
    <div className="flex flex-col h-full bg-white relative">
      <div className="flex-1 overflow-y-auto px-6 py-8 md:px-12 lg:px-20 scrollbar-hide">
        <div className="max-w-2xl mx-auto h-full flex flex-col">
          <StepIndicator currentStep={step} />

          <div className="flex-1 pt-6 pb-20 relative">
            <AnimatePresence mode="wait">
              {step === 1 && (
                <motion.div
                  key="step-1"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: 20 }}
                  transition={{ duration: 0.3 }}
                >
                  <DestinationStep
                    data={data}
                    errors={errors}
                    setFormData={setFormData}
                    nextStep={nextStep}
                    tripLimitInfo={tripLimitInfo}
                  />
                </motion.div>
              )}

              {step === 2 && (
                <motion.div
                  key="step-2"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: 20 }}
                  transition={{ duration: 0.3 }}
                >
                  <PreferencesStep
                    data={data}
                    errors={errors}
                    setFormData={setFormData}
                    nextStep={nextStep}
                    prevStep={prevStep}
                  />
                </motion.div>
              )}

              {step === 3 && (
                <motion.div
                  key="step-3"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: 20 }}
                  transition={{ duration: 0.3 }}
                >
                  <DetailsStep
                    data={data}
                    setFormData={setFormData}
                    prevStep={prevStep}
                    submitForm={submitForm}
                  />
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CreateTripWizard;
