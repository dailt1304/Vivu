import React from 'react';
import { useRouteError, useNavigate } from 'react-router-dom';
import { Home, RefreshCw } from 'lucide-react';

const ErrorBoundary = () => {
    const error = useRouteError();
    const navigate = useNavigate();

    console.error("Route Error:", error);

    return (
        <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-4">
            <div className="max-w-md w-full text-center space-y-6">
                {/* 404 Illustration */}
                <div className="relative w-64 h-64 mx-auto font-display">
                    <div className="absolute inset-0 bg-blue-100 rounded-full blur-3xl opacity-50 animate-pulse"></div>
                    <div className="relative text-9xl font-black text-transparent bg-clip-text bg-linear-to-r from-blue-600 to-cyan-500">
                        {error?.status || 'Ops!'}
                    </div>
                </div>

                <div className="space-y-2">
                    <h1 className="text-2xl font-bold text-gray-900 font-display">
                        {error?.status === 404 ? 'Không tìm thấy trang' : 'Đã có lỗi xảy ra'}
                    </h1>
                    <p className="text-gray-500">
                        {error?.status === 404 
                            ? 'Có vẻ như trang bạn tìm kiếm không tồn tại hoặc đã bị xóa.' 
                            : error?.statusText || error?.message || 'Hệ thống đang gặp sự cố, vui lòng thử lại sau.'}
                    </p>
                </div>

                <div className="flex gap-4 justify-center pt-4">
                     <button 
                        onClick={() => navigate('/')}
                        className="flex items-center gap-2 px-6 py-3 bg-white border border-gray-200 text-gray-700 rounded-xl font-bold hover:bg-gray-50 transition-all shadow-sm"
                    >
                        <Home size={18} />
                        Trang chủ
                    </button>
                    <button 
                        onClick={() => window.location.reload()}
                        className="flex items-center gap-2 px-6 py-3 bg-linear-to-r from-blue-600 to-cyan-600 text-white rounded-xl font-bold hover:shadow-lg hover:shadow-blue-200 transition-all"
                    >
                        <RefreshCw size={18} />
                        Tải lại
                    </button>
                </div>
            </div>
        </div>
    );
};

export default ErrorBoundary;
