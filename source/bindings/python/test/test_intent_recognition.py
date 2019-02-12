import pytest
import azure.cognitiveservices.speech as msspeech

from .conftest import IntentInput
from .utils import _check_intent_result


@pytest.mark.parametrize('intent_input,', ['lamp'], indirect=True)
def test_intent_recognition_simple(intent_input: IntentInput, luis_subscription: str,
                                   luis_region: str, language_understanding_app_id: str):
    audio_config = msspeech.audio.AudioConfig(filename=intent_input.path)
    intent_config = msspeech.SpeechConfig(subscription=luis_subscription, region=luis_region)

    intent_recognizer = msspeech.intent.IntentRecognizer(intent_config, audio_config)

    model = msspeech.intent.LanguageUnderstandingModel(app_id=language_understanding_app_id)
    intent_recognizer.add_intent(model, "HomeAutomation.TurnOn")
    intent_recognizer.add_intent(model, "HomeAutomation.TurnOff")

    intent_recognizer.add_intent("This is a test.", "test")
    intent_recognizer.add_intent("Switch the to channel 34.", "34")
    intent_recognizer.add_intent("what's the weather like", "weather")

    result = intent_recognizer.recognize_once()

    _check_intent_result(result, intent_input, 0)


def test_language_understanding_model_constructor():
    bad_params_error_message = "bad arguments: either pass just an endpoint id, or pass an app " \
            r"id \(with optional subscription and region\)"
    with pytest.raises(ValueError, match=bad_params_error_message):
        lm = msspeech.intent.LanguageUnderstandingModel()

    with pytest.raises(ValueError, match=bad_params_error_message):
        lm = msspeech.intent.LanguageUnderstandingModel(endpoint='a', app_id='b')

    from_subscription_error_message = "all of subscription key, api id and region must be given " \
            "to initialize from subscription"
    with pytest.raises(ValueError, match=from_subscription_error_message):
        lm = msspeech.intent.LanguageUnderstandingModel(subscription='a')

    with pytest.raises(ValueError, match=from_subscription_error_message):
        lm = msspeech.intent.LanguageUnderstandingModel(region='a')

    with pytest.raises(ValueError, match=from_subscription_error_message):
        lm = msspeech.intent.LanguageUnderstandingModel(region='a', app_id='b')

    with pytest.raises(ValueError, match=from_subscription_error_message):
        lm = msspeech.intent.LanguageUnderstandingModel(subscription='a', app_id='b')

    with pytest.raises(ValueError, match=from_subscription_error_message):
        lm = msspeech.intent.LanguageUnderstandingModel('a', 'b')

    lm = msspeech.intent.LanguageUnderstandingModel('a', 'b', 'c')
    assert lm
    lm = msspeech.intent.LanguageUnderstandingModel(app_id='a')
    assert lm
    lm = msspeech.intent.LanguageUnderstandingModel(endpoint='wss://example.com/')
    assert lm
    lm = msspeech.intent.LanguageUnderstandingModel('a', 'b', 'c')
    assert lm


@pytest.mark.parametrize('intent_input,', ['lamp'], indirect=True)
def test_intent_recognition_constructor(intent_input: IntentInput):
    simple_intents = [('This is my first intent', 'Intent1'),
                      ('This is my second intent', 'Intent2'),
                      ('This is my third intent', 'Intent3')]

    model = msspeech.intent.LanguageUnderstandingModel('a', 'b', 'c')
    model_intents = [(model, 'ModelIntent1'),
                     (model, 'ModelIntent2'),
                     (model, 'ModelIntent3')]

    audio_config = msspeech.audio.AudioConfig(filename=intent_input.path)
    intent_config = msspeech.SpeechConfig(subscription='a', region='b')
    intent_recognizer = msspeech.intent.IntentRecognizer(intent_config, audio_config)
    intent_recognizer.add_intents(simple_intents)

    audio_config = msspeech.audio.AudioConfig(filename=intent_input.path)
    intent_config = msspeech.SpeechConfig(subscription='a', region='b')
    intent_recognizer = msspeech.intent.IntentRecognizer(intent_config, audio_config)
    intent_recognizer.add_intents(model_intents)

    audio_config = msspeech.audio.AudioConfig(filename=intent_input.path)
    intent_config = msspeech.SpeechConfig(subscription='a', region='b')
    intent_recognizer = msspeech.intent.IntentRecognizer(intent_config, audio_config)
    intent_recognizer.add_intents(simple_intents + model_intents)


@pytest.mark.parametrize('speech_input,', ['silence'], indirect=True)
def test_intent_recognizer_default_constructor(speech_input):
    """test that the default argument for AudioConfig is correct"""
    speech_config = msspeech.SpeechConfig(subscription="subscription", region="some_region")
    audio_config = msspeech.audio.AudioConfig(filename=speech_input.path)

    reco = msspeech.intent.IntentRecognizer(speech_config, audio_config)
    assert isinstance(reco, msspeech.intent.IntentRecognizer)

    assert "" == reco.authorization_token
    reco.authorization_token = "mytoken"
    assert "mytoken" == reco.authorization_token

    assert isinstance(reco.properties, msspeech.PropertyCollection)
