# Version 1.0
- The Basic Program
# Version 1.5
### New Features  
- **SVG Export**: Added SVG export functionality with a new dependency.  
- **Undo/Redo Functionality**: Implemented proper undo/redo handling and refactored the Action class.  
- **Dynamic Language Detection**: Added automatic language detection for improved user experience.  

#### Improvements  
. **Ease of Use**: Improved usability of canvas, allows users to draw more easily and more quickly.
- **Performance Enhancements**: Optimized drawing functions for better performance and reduced disk space usage with an improved file format.  
- **Keyboard Shortcuts**: Added keyboard shortcuts for enhanced usability.  
- **Event Handling**: Refactored event handlers in `EditForm` and `MainForm` for better maintainability.  
- **Code Quality**: Improved overall code consistency and readability.  

#### Fixes & Maintenance  
- **Settings & Configuration**:  
  - Set `FirstRun` config value to `true` by default.  
  - Added `FirstRun` setting to improve the onboarding experience.  
  - Updated settings file handling by removing unnecessary mentions of `MainForm`.  
- **UI & Naming Adjustments**:  
  - Refactored `PictureBox` creation.  
  - Renamed `NewProjekt` to the correct English name.