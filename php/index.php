<?php
$xmlPath = __DIR__ . '/../xml/contacts.xml';
$xsdPath = __DIR__ . '/../xml/contacts.xsd';
$htmlXsltPath = __DIR__ . '/../xml/html.xslt';
$xmlXsltPath = __DIR__ . '/../xml/xml.xslt';
$jsonPath = __DIR__ . '/contacts.json';

//Загружает XML-файл в DOMDocument и возвращает его.
function loadXml($xmlPath) {
    $doc = new DOMDocument();
    $doc->load($xmlPath);
    return $doc;
}

//Проверяет загруженный XML-документ на соответствие XSD-схеме
function validateXml($doc, $xsdPath) {
    return $doc->schemaValidate($xsdPath);
}

//Преобразует XML в HTML с помощью XSLT-шаблона.
function transformHtml($doc, $htmlXsltPath, $params = []) {
    $xsl = new DOMDocument();
    $xsl->load($htmlXsltPath);
    $proc = new XSLTProcessor();
    $proc->importStylesheet($xsl);
    foreach ($params as $k => $v) { $proc->setParameter('', $k, $v); }
    return $proc->transformToXML($doc);
}

//Преобразует XML в другой XML формат с помощью XSLT-шаблона.
function transformXml($doc, $xmlXsltPath) {
    $xsl = new DOMDocument();
    $xsl->load($xmlXsltPath);
    $proc = new XSLTProcessor();
    $proc->importStylesheet($xsl);
    return $proc->transformToXML($doc);
}

//поиск по имени
function searchByName($xmlPath, $htmlXsltPath, $name) {
    $doc = loadXml($xmlPath);
    return transformHtml($doc, $htmlXsltPath, ['name' => $name]);
}

//поиск по email
function filterByDomain($xmlPath, $htmlXsltPath, $domain) {
    $doc = loadXml($xmlPath);
    return transformHtml($doc, $htmlXsltPath, ['domain' => $domain]);
}

//Читает контакты из XML, превращает в массив, сохраняет в JSON-файл и возвращает массив.
function toJson($xmlPath, $jsonPath) {
    $doc = loadXml($xmlPath);
    $xpath = new DOMXPath($doc);
    $nodes = $xpath->query('/dim1/dim2/dim3/contacts/contact');
    $arr = [];
    foreach ($nodes as $n) {
        $arr[] = [
            'id' => $n->hasAttribute('id') ? $n->getAttribute('id') : '',
            'firstName' => $n->getElementsByTagName('firstName')->item(0)?->nodeValue ?? '',
            'lastName' => $n->getElementsByTagName('lastName')->item(0)?->nodeValue ?? '',
            'phone' => $n->getElementsByTagName('phone')->item(0)?->nodeValue ?? '',
            'email' => $n->getElementsByTagName('email')->item(0)?->nodeValue ?? ''
        ];
    }
    file_put_contents($jsonPath, json_encode($arr, JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT));
    return $arr;
}

//Добавляет новую запись в существующий JSON-файл и возвращает обновлённый массив данных.
function appendJson($jsonPath, $data) {
    $cur = file_exists($jsonPath) ? json_decode(file_get_contents($jsonPath), true) : [];
    $cur[] = $data;
    file_put_contents($jsonPath, json_encode($cur, JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT));
    return $cur;
}

$action = isset($_GET['action']) ? $_GET['action'] : 'html';
if ($action === 'html') {
    $doc = loadXml($xmlPath);
    if (!validateXml($doc, $xsdPath)) { http_response_code(400); echo 'XML invalid'; exit; }
    $name = isset($_GET['name']) ? $_GET['name'] : '';
    $domain = isset($_GET['domain']) ? $_GET['domain'] : '';
    header('Content-Type: text/html; charset=UTF-8');
    echo transformHtml($doc, $htmlXsltPath, ['name' => $name, 'domain' => $domain]);
    exit;
}
if ($action === 'xml') {
    $doc = loadXml($xmlPath);
    header('Content-Type: application/xml; charset=UTF-8');
    echo transformXml($doc, $xmlXsltPath);
    exit;
}
if ($action === 'search') {
    $name = isset($_GET['name']) ? $_GET['name'] : '';
    header('Content-Type: text/html; charset=UTF-8');
    echo searchByName($xmlPath, $htmlXsltPath, $name);
    exit;
}
if ($action === 'filter') {
    $domain = isset($_GET['domain']) ? $_GET['domain'] : '';
    header('Content-Type: text/html; charset=UTF-8');
    echo filterByDomain($xmlPath, $htmlXsltPath, $domain);
    exit;
}
if ($action === 'tojson') {
    header('Content-Type: application/json; charset=UTF-8');
    echo json_encode(toJson($xmlPath, $jsonPath), JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT);
    exit;
}
if ($action === 'append') {
    $data = [
        'id' => isset($_POST['id']) ? $_POST['id'] : '',
        'firstName' => isset($_POST['firstName']) ? $_POST['firstName'] : '',
        'lastName' => isset($_POST['lastName']) ? $_POST['lastName'] : '',
        'phone' => isset($_POST['phone']) ? $_POST['phone'] : '',
        'email' => isset($_POST['email']) ? $_POST['email'] : ''
    ];
    header('Content-Type: application/json; charset=UTF-8');
    echo json_encode(appendJson($jsonPath, $data), JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT);
    exit;
}
http_response_code(404);
echo 'Not found';

