//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
// carbon_csharp_console.cs: A console application for testing Carbon C# client library.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace MicrosoftSpeechSDKSamples
{
    public class SpeechRecognitionSamples
    {
        private static void MyRecognizingEventHandler(object sender, SpeechRecognitionEventArgs e)
        {
            var resultLatency = e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatency);
            Console.WriteLine($"Intermediate result (latency={resultLatency}): {e.ToString()}, Offset: {e.Result.OffsetInTicks}, Duration: {e.Result.Duration}.");
        }

        private static void MyRecognizedEventHandler(object sender, SpeechRecognitionEventArgs e)
        {
            var resultLatency = e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatency);
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"RECOGNIZED (latency={resultLatency}): Text={e.Result.Text}, Offset={e.Result.OffsetInTicks}, Duration={e.Result.Duration}");
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine($"NOMATCH (latency={resultLatency}): Speech could not be recognized. Reason={NoMatchDetails.FromResult(e.Result).Reason}, Offset={e.Result.OffsetInTicks}, Duration={e.Result.Duration}");
            }
            else
            {
                Console.WriteLine($"Unexpected result.(latency={resultLatency}). Reason={e.Result.Reason}, result={e.Result}");
            }
        }

        private static void MyCanceledEventHandler(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            Console.WriteLine($"CANCELED: Reason={e.Reason}");

            if (e.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
            }
        }

        private static void MySpeechStartDetectedEventHandler(object sender, RecognitionEventArgs e)
        {
            Console.WriteLine($"SpeechStartDetected received: offset: {e.Offset}.");
        }

        private static void MySpeechEndDetectedEventHandler(object sender, RecognitionEventArgs e)
        {
            Console.WriteLine($"SpeechEndDetected received: offset: {e.Offset}.");
        }

        private static void MySessionStartedEventHandler(object sender, SessionEventArgs e)
        {
            Console.WriteLine($"Session started event: {e.ToString()}.");
        }

        private static void MySessionStoppedEventHandler(object sender, SessionEventArgs e)
        {
            Console.WriteLine($"Session stopped event: {e.ToString()}.");
        }

        private static void MyConnectedEventHandler(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"Connected event: {e.ToString()}.");
        }
        private static void MyDisconnectedEventHandler(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"Disconnected event: {e.ToString()}.");
        }

        public static async Task SpeechRecognitionBaseModelAsync(string key, string region, string lang, string fileName, bool useStream, bool useToken, bool useContinuousRecognition, string deviceName = null)
        {
            Console.WriteLine("Speech Recognition using base model.");
            SpeechConfig config = null;
            if (useToken)
            {
                config = SpeechConfig.FromAuthorizationToken(key, region);
            }
            else
            {
                config = SpeechConfig.FromSubscription(key, region);
            }
            if (!string.IsNullOrEmpty(lang))
            {
                config.SpeechRecognitionLanguage = lang;
            }
            await RecognizeAsync(config, fileName, useStream, useContinuousRecognition, deviceName).ConfigureAwait(false);
        }

        public static async Task SpeechRecognitionCustomizedModelAsync(string key, string region, string model, string fileName, bool useStream, bool useToken, bool useContinuousRecognition, string deviceName = null)
        {
            Console.WriteLine("Speech Recognition using customized model.");
            SpeechConfig config = null;
            if (useToken)
            {
                config = SpeechConfig.FromAuthorizationToken(key, region);
            }
            else
            {
                config = SpeechConfig.FromSubscription(key, region);
            }
            config.EndpointId = model;

            await RecognizeAsync(config, fileName, useStream, useContinuousRecognition, deviceName).ConfigureAwait(false);
        }

        public static async Task SpeechRecognitionByEndpointAsync(string subscriptionKey, string endpoint, string lang, string model, string fileName, bool useStream, bool useContinuousRecognition, string deviceName = null)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Speech Recognition using endpoint:{0}.", endpoint));

            SpeechConfig config = SpeechConfig.FromEndpoint(new Uri(endpoint), subscriptionKey);
            if (!string.IsNullOrEmpty(lang))
            {
                config.SpeechRecognitionLanguage = lang;
            }

            if (!string.IsNullOrEmpty(model))
            {
                config.EndpointId = model;
            }

            if (!string.IsNullOrEmpty(lang))
            {
                config.SpeechRecognitionLanguage = lang;
            }

            await RecognizeAsync(config, fileName, useStream, useContinuousRecognition, deviceName).ConfigureAwait(false);
        }

        public static async Task RecognizeAsync(SpeechConfig config, string fileName, bool useStream, bool useContinuousRecognition, string deviceName = null)
        {
            if (string.IsNullOrEmpty(fileName) || String.Compare(fileName, "mic", true) == 0)
            {
                var stopWatchCreation = Stopwatch.StartNew();

                AudioConfig audioConfig;
                if (string.IsNullOrEmpty(deviceName))
                {
                    audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                }
                else
                {
                    audioConfig = AudioConfig.FromMicrophoneInput(deviceName);
                }
                using (var reco = new SpeechRecognizer(config, audioConfig))
                {
                    stopWatchCreation.Stop();
                    Console.WriteLine("Time for creating speech recognizer: " + stopWatchCreation.ElapsedMilliseconds);
                    await LaunchRecognizerAsync(reco, useContinuousRecognition);
                }
            }
            else
            {
                if (useStream)
                {
                    Console.WriteLine("Using stream input.");
                    var audioInput = Util.OpenWavFile(fileName);
                    var stopWatchCreation = Stopwatch.StartNew();
                    using (var reco = new SpeechRecognizer(config, audioInput))
                    {
                        stopWatchCreation.Stop();
                        Console.WriteLine("Time for creating speech recognizer: " + stopWatchCreation.ElapsedMilliseconds);
                        await LaunchRecognizerAsync(reco, useContinuousRecognition);
                    }
                }
                else
                {
                    var stopWatchCreation = Stopwatch.StartNew();
                    using (var reco = new SpeechRecognizer(config, AudioConfig.FromWavFileInput(fileName)))
                    {
                        stopWatchCreation.Stop();
                        Console.WriteLine("Time for creating speech recognizer: " + stopWatchCreation.ElapsedMilliseconds);
                        await LaunchRecognizerAsync(reco, useContinuousRecognition);
                    }
                }
            }
        }

        private static async Task LaunchRecognizerAsync(SpeechRecognizer reco, bool useContinuousRecognition)
        {
            if (useContinuousRecognition)
            {
                await ContinuousRecognitionAsync(reco).ConfigureAwait(false);
            }
            else
            {
                await SingleShotRecognitionAsync(reco).ConfigureAwait(false);
            }
        }

        private static async Task SingleShotRecognitionAsync(SpeechRecognizer reco)
        {
            Console.WriteLine("Single-shot recognition.");
            var connection = Connection.FromRecognizer(reco);

            // Subscribes to events.
            connection.Connected += MyConnectedEventHandler;
            connection.Disconnected += MyDisconnectedEventHandler;
            reco.Recognizing += MyRecognizingEventHandler;
            reco.Recognized += MyRecognizedEventHandler;
            reco.Canceled += MyCanceledEventHandler;
            reco.SpeechStartDetected += MySpeechStartDetectedEventHandler;
            reco.SpeechEndDetected += MySpeechEndDetectedEventHandler;
            reco.SessionStarted += MySessionStartedEventHandler;
            reco.SessionStopped += MySessionStoppedEventHandler;

            // Starts recognition.
            var result = await reco.RecognizeOnceAsync().ConfigureAwait(false);

            Console.WriteLine("Speech Recognition: Recognition result: " + result);

            // Unsubscribe to events.
            connection.Connected -= MyConnectedEventHandler;
            connection.Disconnected -= MyDisconnectedEventHandler;
            reco.Recognizing -= MyRecognizingEventHandler;
            reco.Recognized -= MyRecognizedEventHandler;
            reco.Canceled -= MyCanceledEventHandler;
            reco.SpeechStartDetected -= MySpeechStartDetectedEventHandler;
            reco.SpeechEndDetected -= MySpeechEndDetectedEventHandler;
            reco.SessionStarted -= MySessionStartedEventHandler;
            reco.SessionStopped -= MySessionStoppedEventHandler;
        }

        private static async Task ContinuousRecognitionAsync(SpeechRecognizer reco)
        {
            Console.WriteLine("Continuous recognition.");
            var tcsLocal = new TaskCompletionSource<int>();
            var connection = Connection.FromRecognizer(reco);
            connection.Connected += MyConnectedEventHandler;
            connection.Disconnected += MyDisconnectedEventHandler;
            reco.Recognizing += MyRecognizingEventHandler;
            reco.Recognized += MyRecognizedEventHandler;
            reco.Canceled += MyCanceledEventHandler;
            reco.SessionStarted += MySessionStartedEventHandler;
            reco.SpeechStartDetected += MySpeechStartDetectedEventHandler;
            reco.SpeechEndDetected += MySpeechEndDetectedEventHandler;
            reco.SessionStopped += (s, e) =>
            {
                MySessionStoppedEventHandler(s, e);
                Console.WriteLine($"Session Stop detected. Stop the recognition.");
                tcsLocal.TrySetResult(0);
            };

            // Starts continuos recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            await reco.StartContinuousRecognitionAsync().ConfigureAwait(false);

            // Waits for completion.
            // Use Task.WaitAny to make sure that the task is rooted.
            Task.WaitAny(new[] { tcsLocal.Task });

            // Stops translation.
            await reco.StopContinuousRecognitionAsync().ConfigureAwait(false);

            connection.Connected -= MyConnectedEventHandler;
            connection.Disconnected -= MyDisconnectedEventHandler;
            reco.Recognizing -= MyRecognizingEventHandler;
            reco.Recognized -= MyRecognizedEventHandler;
            reco.Canceled -= MyCanceledEventHandler;
            reco.SpeechStartDetected -= MySpeechStartDetectedEventHandler;
            reco.SpeechEndDetected -= MySpeechEndDetectedEventHandler;
            reco.SessionStarted -= MySessionStartedEventHandler;
        }
    }
}