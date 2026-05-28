import React from 'react';
import { WifiOff, RefreshCw } from 'lucide-react';

/**
 * Inline error component for connection failures
 * Use this inside your page layout to show error state while preserving navbar/layout
 */
const ConnectionError = ({ message, onRetry, isRetrying = false }) => {
    return (
        <div className="flex-1 flex items-center justify-center p-8">
            <div className="text-center space-y-6 max-w-md">
                {/* Icon */}
                <div className="mx-auto w-20 h-20 rounded-full bg-linear-to-br from-red-50 to-orange-50 flex items-center justify-center">
                    <WifiOff className="w-10 h-10 text-red-400" />
                </div>
                
                {/* Message */}
                <div className="space-y-2">
                    <h3 className="text-xl font-bold text-gray-800">
                        Không thể kết nối
                    </h3>
                    <p className="text-gray-500 text-sm">
                        {message || 'Đã xảy ra lỗi khi tải dữ liệu. Vui lòng kiểm tra kết nối mạng và thử lại.'}
                    </p>
                </div>
                
                {/* Retry Button */}
                <button
                    onClick={onRetry}
                    disabled={isRetrying}
                    className="inline-flex items-center gap-2 px-6 py-3 bg-linear-to-r from-blue-600 to-cyan-600 text-white rounded-full font-medium hover:shadow-lg hover:shadow-blue-200 transition-all disabled:opacity-50"
                >
                    <RefreshCw className={`w-5 h-5 ${isRetrying ? 'animate-spin' : ''}`} />
                    {isRetrying ? 'Đang thử lại...' : 'Thử lại'}
                </button>
            </div>
        </div>
    );
};

export default ConnectionError;
