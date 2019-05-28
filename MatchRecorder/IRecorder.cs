using DuckGame;
using MatchTracker;

namespace MatchRecorder
{
	internal interface IRecorder
	{
		bool IsRecording { get; }
		RecordingType ResultingRecordingType { get; set; }

		void StartRecording();

		void StopRecording();

		void Update();
		void StartFrame();
		void EndFrame();
		void OnTextureDraw( Tex2D texture , DuckGame.Vec2 position , DuckGame.Rectangle? sourceRectangle , DuckGame.Color color , float rotation , DuckGame.Vec2 origin , DuckGame.Vec2 scale , int effects , Depth depth = default( Depth ) );
	
        int OnStartStaticDraw();
        void OnFinishStaticDraw();
        void OnStaticDraw( int id );

        bool WantsTexture( object tex );
        void SendTextureData( object tex, int width, int height, byte[] data );

		void OnStartDrawingObject( object obj );
		void OnFinishDrawingObject( object obj );
    }
}