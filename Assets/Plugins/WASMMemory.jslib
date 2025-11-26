mergeInto(LibraryManager.library, {
    GetWasmTotal: function () {
        // 整个 WASM 线性内存的 byteLength（和 TotalHeapMemory 对应）
        try {
            var heap = (typeof HEAPU8 !== 'undefined') ? HEAPU8
                : (typeof Module !== 'undefined' && Module.HEAPU8) ? Module.HEAPU8
                    : null;
            if (heap && heap.buffer && typeof heap.buffer.byteLength === 'number') {
                return heap.buffer.byteLength;
            }
        } catch (e) {
            console.error('GetWasmTotal error:', e);
        }
        return 0;
    },

    GetWasmUsed: function () {
        // 当前 break 指针 = dynamic memory 已用大小
        // 某些 Unity 版本可能没有 _sbrk，可以加个保护
        try {
            if (typeof _sbrk === 'function') {
                return _sbrk(0);
            }
            if (typeof Module !== 'undefined' && typeof Module['_sbrk'] === 'function') {
                return Module['_sbrk'](0);
            }
        } catch (e) {
            console.error('GetWasmUsed error:', e);
        }
        return 0;
    },

    GetJSHeapUsed: function () {
        try {
            if (typeof performance !== 'undefined' &&
                performance.memory &&
                typeof performance.memory.usedJSHeapSize === 'number') {
                return performance.memory.usedJSHeapSize;
            }
        } catch (e) {
            console.error('GetJSHeapUsed error:', e);
        }
        return 0;
    },

    GetJSHeapTotal: function () {
        try {
            if (typeof performance !== 'undefined' &&
                performance.memory &&
                typeof performance.memory.totalJSHeapSize === 'number') {
                return performance.memory.totalJSHeapSize;
            }
        } catch (e) {
            console.error('GetJSHeapTotal error:', e);
        }
        return 0;
    },

    GetJSHeapUsedWX: function () {
        try {
            // 1) 微信小游戏专用
            if (typeof wx !== 'undefined' &&
                wx.getPerformance &&
                wx.getPerformance().memory &&
                typeof wx.getPerformance().memory.usedJSHeapSize === 'number') {
                return wx.getPerformance().memory.usedJSHeapSize;
            }

            // 2) 退回浏览器 performance.memory（比如你在普通 WebGL 场景里）
            if (typeof performance !== 'undefined' &&
                performance.memory &&
                typeof performance.memory.usedJSHeapSize === 'number') {
                return performance.memory.usedJSHeapSize;
            }
        } catch (e) {
            console.error('GetJSHeapUsed error:', e);
        }
        return 0;
    },

    GetJSHeapTotalWX: function () {
        try {
            if (typeof wx !== 'undefined' &&
                wx.getPerformance &&
                wx.getPerformance().memory &&
                typeof wx.getPerformance().memory.totalJSHeapSize === 'number') {
                return wx.getPerformance().memory.totalJSHeapSize;
            }

            if (typeof performance !== 'undefined' &&
                performance.memory &&
                typeof performance.memory.totalJSHeapSize === 'number') {
                return performance.memory.totalJSHeapSize;
            }
        } catch (e) {
            console.error('GetJSHeapTotal error:', e);
        }
        return 0;
    }
});
